using Keepwise.Application.Abstractions;
using Keepwise.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Keepwise.Infrastructure.Persistence;

public sealed class KeepwiseDbContext(DbContextOptions<KeepwiseDbContext> options)
    : DbContext(options), IKeepwiseDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<UserDevice> UserDevices => Set<UserDevice>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<ItemType> ItemTypes => Set<ItemType>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<Coverage> Coverages => Set<Coverage>();
    public DbSet<ReminderRule> ReminderRules => Set<ReminderRule>();
    public DbSet<ReminderOccurrence> ReminderOccurrences => Set<ReminderOccurrence>();
    public DbSet<MaintenanceEvent> MaintenanceEvents => Set<MaintenanceEvent>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<PurchaseCandidate> PurchaseCandidates => Set<PurchaseCandidate>();
    public DbSet<IngestionJob> IngestionJobs => Set<IngestionJob>();
    public DbSet<UserIngestionSettings> UserIngestionSettings => Set<UserIngestionSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(KeepwiseDbContext).Assembly);
    }
}
