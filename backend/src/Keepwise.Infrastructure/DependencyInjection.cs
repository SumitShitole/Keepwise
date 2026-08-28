using Hangfire;
using Hangfire.PostgreSql;
using Keepwise.Application;
using Keepwise.Application.Abstractions;
using Keepwise.Domain;
using Keepwise.Infrastructure.Jobs;
using Keepwise.Infrastructure.Notifications;
using Keepwise.Infrastructure.Persistence;
using Keepwise.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Keepwise.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddKeepwiseInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var connectionString = configuration.GetConnectionString("Keepwise")
            ?? throw new InvalidOperationException("Connection string 'Keepwise' is not configured.");

        services.AddDbContext<KeepwiseDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly("Keepwise.Infrastructure")));
        services.AddScoped<IKeepwiseDbContext>(sp => sp.GetRequiredService<KeepwiseDbContext>());

        var storageRoot = configuration["Storage:Root"] ?? Path.Combine(Path.GetTempPath(), "keepwise-uploads");
        services.AddSingleton<IFileStorage>(_ => new LocalFileStorage(storageRoot));

        services.AddSingleton<INotificationSender>(sp =>
            new LoggingNotificationSender(sp.GetRequiredService<ILogger<LoggingNotificationSender>>(), NotificationChannel.Email));
        services.AddSingleton<INotificationSender>(sp =>
            new LoggingNotificationSender(sp.GetRequiredService<ILogger<LoggingNotificationSender>>(), NotificationChannel.Push));
        services.AddSingleton<INotificationSender, StubSmsSender>();
        services.AddSingleton<INotificationSender, StubWhatsAppSender>();

        services.AddTransient<ReminderJobs>();

        if (!environment.IsEnvironment("Testing"))
        {
            services.AddHangfire(config => config.UsePostgreSqlStorage(options =>
                options.UseNpgsqlConnection(connectionString)));
            services.AddHangfireServer(options =>
            {
                options.SchedulePollingInterval = TimeSpan.FromSeconds(15);
                options.WorkerCount = Math.Max(1, Environment.ProcessorCount / 2);
            });
        }

        return services;
    }
}
