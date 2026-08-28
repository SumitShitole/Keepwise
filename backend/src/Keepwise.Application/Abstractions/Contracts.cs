using Keepwise.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Keepwise.Application.Abstractions;

public interface IKeepwiseDbContext
{
    DbSet<User> Users { get; }
    DbSet<UserDevice> UserDevices { get; }
    DbSet<Category> Categories { get; }
    DbSet<ItemType> ItemTypes { get; }
    DbSet<Item> Items { get; }
    DbSet<Coverage> Coverages { get; }
    DbSet<ReminderRule> ReminderRules { get; }
    DbSet<ReminderOccurrence> ReminderOccurrences { get; }
    DbSet<MaintenanceEvent> MaintenanceEvents { get; }
    DbSet<Attachment> Attachments { get; }
    DbSet<AuditEvent> AuditEvents { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

public interface ICurrentUser
{
    Guid UserId { get; }
    string Email { get; }
    bool IsAuthenticated { get; }
}

public interface INotificationSender
{
    NotificationChannel Channel { get; }
    Task SendAsync(NotificationMessage message, CancellationToken cancellationToken);
}

public sealed record NotificationMessage(
    Guid UserId,
    string To,
    string Subject,
    string Body,
    NotificationChannel Channel);

public interface IFileStorage
{
    Task<string> SaveAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken);
    Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken);
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken);
}
