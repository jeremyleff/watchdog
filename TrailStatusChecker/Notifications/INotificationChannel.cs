namespace Watchdog.Notifications;

public interface INotificationChannel
{
    string Id { get; }
    Task SendAsync(NotificationMessage message, CancellationToken ct = default);
}
