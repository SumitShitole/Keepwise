using Keepwise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Keepwise.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.FirebaseUid).IsUnique();
        builder.HasIndex(x => x.Email);
        builder.Property(x => x.Email).HasMaxLength(320).IsRequired();
        builder.Property(x => x.FirebaseUid).HasMaxLength(128).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(120).IsRequired();
        builder.Property(x => x.MobileNumber).HasMaxLength(32);
        builder.Property(x => x.CountryCode).HasMaxLength(2).IsRequired();
        builder.Property(x => x.TimeZoneId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Language).HasMaxLength(16).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasQueryFilter(x => x.DeletedAtUtc == null);
    }
}

internal sealed class UserDeviceConfiguration : IEntityTypeConfiguration<UserDevice>
{
    public void Configure(EntityTypeBuilder<UserDevice> builder)
    {
        builder.ToTable("user_devices");
        builder.HasIndex(x => new { x.UserId, x.PushToken }).IsUnique();
        builder.Property(x => x.PushToken).HasMaxLength(512).IsRequired();
        builder.Property(x => x.Platform).HasMaxLength(32).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasOne(x => x.User).WithMany(x => x.Devices).HasForeignKey(x => x.UserId);
    }
}

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.Property(x => x.Name).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(80).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasMany(x => x.ItemTypes).WithOne(x => x.Category).HasForeignKey(x => x.CategoryId);
    }
}

internal sealed class ItemTypeConfiguration : IEntityTypeConfiguration<ItemType>
{
    public void Configure(EntityTypeBuilder<ItemType> builder)
    {
        builder.ToTable("item_types");
        builder.HasIndex(x => new { x.CategoryId, x.Slug }).IsUnique();
        builder.Property(x => x.Name).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(80).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}

internal sealed class ItemConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.ToTable("items");
        builder.HasIndex(x => new { x.UserId, x.DeletedAtUtc });
        builder.HasIndex(x => x.Name);
        builder.Property(x => x.Name).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Brand).HasMaxLength(80);
        builder.Property(x => x.ModelNumber).HasMaxLength(80);
        builder.Property(x => x.SerialNumber).HasMaxLength(80);
        builder.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        builder.Property(x => x.VendorName).HasMaxLength(120);
        builder.Property(x => x.VendorContact).HasMaxLength(120);
        builder.Property(x => x.PurchasePrice).HasPrecision(12, 2);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasQueryFilter(x => x.DeletedAtUtc == null);
        builder.HasOne(x => x.User).WithMany(x => x.Items).HasForeignKey(x => x.UserId);
        builder.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.ItemType).WithMany().HasForeignKey(x => x.ItemTypeId).OnDelete(DeleteBehavior.SetNull);
    }
}

internal sealed class CoverageConfiguration : IEntityTypeConfiguration<Coverage>
{
    public void Configure(EntityTypeBuilder<Coverage> builder)
    {
        builder.ToTable("coverages");
        builder.HasIndex(x => new { x.ItemId, x.Kind, x.Status });
        builder.HasIndex(x => x.EndDate);
        builder.Property(x => x.Title).HasMaxLength(160);
        builder.Property(x => x.Provider).HasMaxLength(160);
        builder.Property(x => x.ReferenceNumber).HasMaxLength(80);
        builder.Property(x => x.Premium).HasPrecision(12, 2);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasQueryFilter(x => x.DeletedAtUtc == null);
        builder.HasOne(x => x.Item).WithMany(x => x.Coverages).HasForeignKey(x => x.ItemId);
    }
}

internal sealed class ReminderRuleConfiguration : IEntityTypeConfiguration<ReminderRule>
{
    public void Configure(EntityTypeBuilder<ReminderRule> builder)
    {
        builder.ToTable("reminder_rules");
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasOne(x => x.Coverage).WithMany(x => x.ReminderRules).HasForeignKey(x => x.CoverageId);
    }
}

internal sealed class ReminderOccurrenceConfiguration : IEntityTypeConfiguration<ReminderOccurrence>
{
    public void Configure(EntityTypeBuilder<ReminderOccurrence> builder)
    {
        builder.ToTable("reminder_occurrences");
        builder.HasIndex(x => x.OccurrenceKey).IsUnique();
        builder.HasIndex(x => new { x.Status, x.ScheduledAtUtc });
        builder.Property(x => x.OccurrenceKey).HasMaxLength(160).IsRequired();
        builder.Property(x => x.LastError).HasMaxLength(1000);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasOne(x => x.Coverage).WithMany().HasForeignKey(x => x.CoverageId);
        builder.HasOne(x => x.ReminderRule).WithMany().HasForeignKey(x => x.ReminderRuleId);
    }
}

internal sealed class MaintenanceEventConfiguration : IEntityTypeConfiguration<MaintenanceEvent>
{
    public void Configure(EntityTypeBuilder<MaintenanceEvent> builder)
    {
        builder.ToTable("maintenance_events");
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasOne(x => x.Coverage).WithMany(x => x.MaintenanceEvents).HasForeignKey(x => x.CoverageId);
    }
}

internal sealed class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
{
    public void Configure(EntityTypeBuilder<Attachment> builder)
    {
        builder.ToTable("attachments");
        builder.Property(x => x.FileName).HasMaxLength(260).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(120).IsRequired();
        builder.Property(x => x.StorageKey).HasMaxLength(512).IsRequired();
        builder.Property(x => x.Sha256).HasMaxLength(64);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasQueryFilter(x => x.DeletedAtUtc == null);
        builder.HasOne(x => x.Item).WithMany(x => x.Attachments).HasForeignKey(x => x.ItemId);
    }
}

internal sealed class PurchaseConfiguration : IEntityTypeConfiguration<Purchase>
{
    public void Configure(EntityTypeBuilder<Purchase> builder)
    {
        builder.ToTable("purchases");
        builder.HasIndex(x => x.ItemId).IsUnique();
        builder.HasIndex(x => new { x.UserId, x.Fingerprint });
        builder.Property(x => x.VendorName).HasMaxLength(160);
        builder.Property(x => x.OrderNumber).HasMaxLength(80);
        builder.Property(x => x.InvoiceNumber).HasMaxLength(80);
        builder.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        builder.Property(x => x.Gstin).HasMaxLength(20);
        builder.Property(x => x.UpiReference).HasMaxLength(80);
        builder.Property(x => x.Fingerprint).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(12, 2);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasOne(x => x.Item).WithOne(x => x.Purchase).HasForeignKey<Purchase>(x => x.ItemId);
    }
}

internal sealed class PurchaseCandidateConfiguration : IEntityTypeConfiguration<PurchaseCandidate>
{
    public void Configure(EntityTypeBuilder<PurchaseCandidate> builder)
    {
        builder.ToTable("purchase_candidates");
        builder.HasIndex(x => new { x.UserId, x.Status });
        builder.HasIndex(x => new { x.UserId, x.Fingerprint });
        builder.HasIndex(x => x.Sha256);
        builder.Property(x => x.Fingerprint).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Sha256).HasMaxLength(64);
        builder.Property(x => x.StorageKey).HasMaxLength(512);
        builder.Property(x => x.PayloadJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasQueryFilter(x => x.DeletedAtUtc == null);
    }
}

internal sealed class IngestionJobConfiguration : IEntityTypeConfiguration<IngestionJob>
{
    public void Configure(EntityTypeBuilder<IngestionJob> builder)
    {
        builder.ToTable("ingestion_jobs");
        builder.HasIndex(x => new { x.UserId, x.Status });
        builder.HasIndex(x => x.Sha256);
        builder.Property(x => x.ContentType).HasMaxLength(120);
        builder.Property(x => x.StorageKey).HasMaxLength(512);
        builder.Property(x => x.Sha256).HasMaxLength(64);
        builder.Property(x => x.ErrorCode).HasMaxLength(80);
        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}

internal sealed class UserIngestionSettingsConfiguration : IEntityTypeConfiguration<UserIngestionSettings>
{
    public void Configure(EntityTypeBuilder<UserIngestionSettings> builder)
    {
        builder.ToTable("user_ingestion_settings");
        builder.HasIndex(x => x.UserId).IsUnique();
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
    }
}

internal sealed class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    public void Configure(EntityTypeBuilder<AuditEvent> builder)
    {
        builder.ToTable("audit_events");
        builder.Property(x => x.Action).HasMaxLength(80).IsRequired();
        builder.Property(x => x.EntityType).HasMaxLength(80).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}
