using Keepwise.Application.Abstractions;
using Keepwise.Domain;

namespace Keepwise.Infrastructure.Notifications;

public sealed class LoggingNotificationSender(ILogger<LoggingNotificationSender> logger, NotificationChannel channel)
    : INotificationSender
{
    public NotificationChannel Channel { get; } = channel;

    public Task SendAsync(NotificationMessage message, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Dev notification {Channel} to {To}: {Subject} — {Body}",
            Channel,
            message.To,
            message.Subject,
            message.Body);
        return Task.CompletedTask;
    }
}

public sealed class StubSmsSender(ILogger<StubSmsSender> logger) : INotificationSender
{
    public NotificationChannel Channel => NotificationChannel.Sms;

    public Task SendAsync(NotificationMessage message, CancellationToken cancellationToken)
    {
        logger.LogInformation("SMS provider stub skipped for {To}", message.To);
        return Task.CompletedTask;
    }
}

public sealed class StubWhatsAppSender(ILogger<StubWhatsAppSender> logger) : INotificationSender
{
    public NotificationChannel Channel => NotificationChannel.WhatsApp;

    public Task SendAsync(NotificationMessage message, CancellationToken cancellationToken)
    {
        logger.LogInformation("WhatsApp provider stub skipped for {To}", message.To);
        return Task.CompletedTask;
    }
}
