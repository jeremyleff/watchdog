namespace Watchdog.Notifications;

public sealed record NotificationMessage(
    string MonitorId,
    string MonitorDisplayName,
    string Subject,
    string Body,
    string? PreviousValue,
    string NewValue,
    DateTimeOffset Timestamp
);
