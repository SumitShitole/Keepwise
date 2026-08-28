using System.Text;
using System.Threading.RateLimiting;
using Hangfire;
using Keepwise.Api.Auth;
using Keepwise.Api.Middleware;
using Keepwise.Application;
using Keepwise.Application.Abstractions;
using Keepwise.Infrastructure;
using Keepwise.Infrastructure.Jobs;
using Keepwise.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, services, config) =>
    config.ReadFrom.Configuration(ctx.Configuration).Enrich.FromLogContext().WriteTo.Console());

builder.Services.AddKeepwiseApplication();
builder.Services.AddKeepwiseInfrastructure(builder.Configuration, builder.Environment);
builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection(AuthOptions.SectionName));
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("auth", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1)
            }));
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

var auth = builder.Configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        if (!string.IsNullOrWhiteSpace(auth.FirebaseProjectId) && !auth.AllowDevLogin)
        {
            options.Authority = $"https://securetoken.google.com/{auth.FirebaseProjectId}";
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = $"https://securetoken.google.com/{auth.FirebaseProjectId}",
                ValidateAudience = true,
                ValidAudience = auth.FirebaseProjectId,
                ValidateLifetime = true
            };
        }
        else
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = auth.DevIssuer,
                ValidateAudience = true,
                ValidAudience = auth.DevAudience,
                ValidateLifetime = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(auth.DevSigningKey)),
                ClockSkew = TimeSpan.FromMinutes(1)
            };
        }
    });
builder.Services.AddAuthorization();

var webOrigin = builder.Configuration["WebOrigin"] ?? "http://127.0.0.1:43123";
builder.Services.AddCors(options =>
    options.AddPolicy("web", policy =>
        policy.WithOrigins(webOrigin, "http://localhost:43123")
            .AllowAnyHeader()
            .AllowAnyMethod()));

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors("web");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<KeepwiseDbContext>();
    await db.Database.MigrateAsync();
    CatalogSeed.Ensure(db);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseHangfireDashboard("/hangfire");
}

app.MapControllers();
app.MapHealthChecks("/health");

if (!app.Environment.IsEnvironment("Testing"))
{
    var recurring = app.Services.GetRequiredService<IRecurringJobManager>();
    recurring.AddOrUpdate<ReminderJobs>(
        "reminders-generate",
        job => job.Generate(CancellationToken.None),
        "*/5 * * * *");
    recurring.AddOrUpdate<ReminderJobs>(
        "reminders-dispatch",
        job => job.Dispatch(CancellationToken.None),
        "* * * * *");
    recurring.AddOrUpdate<ReminderJobs>(
        "coverage-status-refresh",
        job => job.RefreshStatuses(CancellationToken.None),
        "15 * * * *");
}

app.Run();

public partial class Program;
