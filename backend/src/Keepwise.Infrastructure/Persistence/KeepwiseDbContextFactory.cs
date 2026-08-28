using Keepwise.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Keepwise.Infrastructure.Persistence;

public sealed class KeepwiseDbContextFactory : IDesignTimeDbContextFactory<KeepwiseDbContext>
{
    public KeepwiseDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../Keepwise.Api"))
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("Keepwise")
            ?? "Host=127.0.0.1;Port=5432;Database=keepwise;Username=keepwise;Password=keepwise_dev";

        var options = new DbContextOptionsBuilder<KeepwiseDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new KeepwiseDbContext(options);
    }
}
