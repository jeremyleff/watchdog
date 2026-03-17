using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Watchdog.Monitors;
using Watchdog.Notifications;
using Watchdog.State;

namespace Watchdog.Orchestration;

public class MonitorOrchestrator : BackgroundService
{
    private readonly MonitorFactory _monitorFactory;
    private readonly NotificationChannelFactory _channelFactory;
    private readonly IStateStore _stateStore;
    private readonly ILogger<MonitorOrchestrator> _logger;

    public MonitorOrchestrator(
        MonitorFactory monitorFactory,
        NotificationChannelFactory channelFactory,
        IStateStore stateStore,
        ILogger<MonitorOrchestrator> logger)
    {
        _monitorFactory = monitorFactory;
        _channelFactory = channelFactory;
        _stateStore = stateStore;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var monitors = _monitorFactory.GetAll();

        if (monitors.Count == 0)
        {
            _logger.LogWarning("No monitors configured. Service will idle.");
            await Task.Delay(Timeout.Infinite, stoppingToken);
            return;
        }

        _logger.LogInformation("Starting {Count} monitor(s).", monitors.Count);

        // Send startup notifications and launch per-monitor loops concurrently
        var tasks = monitors.Select(m => RunMonitorAsync(m, stoppingToken)).ToArray();
        await Task.WhenAll(tasks);
    }

    private async Task RunMonitorAsync(IMonitor monitor, CancellationToken ct)
    {
        var lastKnown = await _stateStore.GetAsync(monitor.Id, ct);

        if (monitor.NotifyOnStart)
        {
            var startBody = lastKnown is null
                ? $"{monitor.DisplayName} monitor started. No previous status on record."
                : $"{monitor.DisplayName} monitor started. Last known status: {lastKnown}";

            await NotifyAsync(monitor, new NotificationMessage(
                MonitorId: monitor.Id,
                MonitorDisplayName: monitor.DisplayName,
                Subject: $"{monitor.DisplayName} – Monitor Started",
                Body: startBody,
                PreviousValue: null,
                NewValue: lastKnown ?? "",
                Timestamp: DateTimeOffset.UtcNow
            ), ct);
        }

        using var timer = new PeriodicTimer(monitor.Interval);
        while (await timer.WaitForNextTickAsync(ct))
        {
            await CheckAndNotifyAsync(monitor, ct);
        }
    }

    private async Task CheckAndNotifyAsync(IMonitor monitor, CancellationToken ct)
    {
        MonitorResult result;
        try
        {
            result = await monitor.CheckAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in monitor '{MonitorId}'", monitor.Id);
            return;
        }

        if (!result.Success)
        {
            _logger.LogWarning("Monitor '{MonitorId}' check failed: {Error}", monitor.Id, result.ErrorMessage);
            return;
        }

        var stored = await _stateStore.GetAsync(monitor.Id, ct);

        if (result.Value == stored)
        {
            _logger.LogDebug("Monitor '{MonitorId}' — no change ({Value})", monitor.Id, result.Value);
            return;
        }

        _logger.LogInformation("Monitor '{MonitorId}' status changed: '{Old}' → '{New}'", monitor.Id, stored ?? "(none)", result.Value);
        await _stateStore.SetAsync(monitor.Id, result.Value, ct);

        var message = new NotificationMessage(
            MonitorId: monitor.Id,
            MonitorDisplayName: monitor.DisplayName,
            Subject: $"{monitor.DisplayName} – Status Changed",
            Body: $"{monitor.DisplayName} status changed.\n\nPrevious: {stored ?? "(none)"}\nCurrent:  {result.Value}",
            PreviousValue: stored,
            NewValue: result.Value,
            Timestamp: DateTimeOffset.UtcNow
        );

        await NotifyAsync(monitor, message, ct);
    }

    private async Task NotifyAsync(IMonitor monitor, NotificationMessage message, CancellationToken ct)
    {
        foreach (var channelId in monitor.NotificationChannelIds)
        {
            var channel = _channelFactory.GetById(channelId);
            if (channel is null)
            {
                _logger.LogWarning("Monitor '{MonitorId}' references unknown channel '{ChannelId}'", monitor.Id, channelId);
                continue;
            }

            try
            {
                await channel.SendAsync(message, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Channel '{ChannelId}' threw while sending notification for monitor '{MonitorId}'", channelId, monitor.Id);
            }
        }
    }
}
