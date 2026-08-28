using Keepwise.Application.Abstractions;
using Keepwise.Application.Catalog;
using Keepwise.Application.Dashboard;
using Keepwise.Application.Documents;
using Keepwise.Application.Identity;
using Keepwise.Application.Items;
using Keepwise.Application.Reminders;
using Microsoft.Extensions.DependencyInjection;

namespace Keepwise.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddKeepwiseApplication(this IServiceCollection services)
    {
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<CatalogService>();
        services.AddScoped<UserService>();
        services.AddScoped<CoverageFactory>();
        services.AddScoped<ItemService>();
        services.AddScoped<CoverageService>();
        services.AddScoped<DashboardService>();
        services.AddScoped<ReminderEngine>();
        services.AddScoped<DocumentService>();
        return services;
    }
}
