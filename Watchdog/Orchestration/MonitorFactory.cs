using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Watchdog.Configuration;
using Watchdog.Monitors;

namespace Watchdog.Orchestration;

public class MonitorFactory
{
    private readonly IReadOnlyList<IMonitor> _monitors;

    public MonitorFactory(
        IOptions<NotificationMonitorOptions> options,
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory)
    {
        var monitors = new List<IMonitor>();
        foreach (var config in options.Value.Monitors)
        {
            IMonitor monitor = config.Type switch
            {
                "WebContent" => new WebContentMonitor(config, httpClientFactory, loggerFactory.CreateLogger<WebContentMonitor>()),
                "HttpStatus" => new HttpStatusMonitor(config, httpClientFactory, loggerFactory.CreateLogger<HttpStatusMonitor>()),
                "KeywordPresence" => new KeywordPresenceMonitor(config, httpClientFactory, loggerFactory.CreateLogger<KeywordPresenceMonitor>()),
                _ => throw new InvalidOperationException($"Unknown monitor type '{config.Type}' for monitor '{config.Id}'.")
            };
            monitors.Add(monitor);
        }
        _monitors = monitors.AsReadOnly();
    }

    public IReadOnlyList<IMonitor> GetAll() => _monitors;
}
