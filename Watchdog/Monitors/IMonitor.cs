namespace Watchdog.Monitors;

public interface IMonitor
{
    string Id { get; }
    string DisplayName { get; }
    TimeSpan Interval { get; }
    bool NotifyOnStart { get; }
    IReadOnlyList<string> NotificationChannelIds { get; }
    Task<MonitorResult> CheckAsync(CancellationToken ct = default);
}
