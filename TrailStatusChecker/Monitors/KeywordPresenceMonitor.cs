using Microsoft.Extensions.Logging;
using Watchdog.Configuration;

namespace Watchdog.Monitors;

public class KeywordPresenceMonitor : IMonitor
{
    private readonly KeywordPresenceMonitorOptions _options;
    private readonly HttpClient _httpClient;
    private readonly ILogger<KeywordPresenceMonitor> _logger;

    public string Id { get; }
    public string DisplayName { get; }
    public TimeSpan Interval { get; }
    public bool NotifyOnStart { get; }
    public IReadOnlyList<string> NotificationChannelIds { get; }

    public KeywordPresenceMonitor(MonitorConfig config, IHttpClientFactory httpClientFactory, ILogger<KeywordPresenceMonitor> logger)
    {
        Id = config.Id;
        DisplayName = config.DisplayName;
        Interval = TimeSpan.FromSeconds(config.IntervalSeconds);
        NotifyOnStart = config.NotifyOnStart;
        NotificationChannelIds = config.ChannelIds.AsReadOnly();
        _options = config.KeywordPresence ?? throw new ArgumentException($"Monitor '{config.Id}' is missing KeywordPresence configuration.");
        _httpClient = httpClientFactory.CreateClient();
        _logger = logger;
    }

    public async Task<MonitorResult> CheckAsync(CancellationToken ct = default)
    {
        try
        {
            var content = await _httpClient.GetStringAsync(_options.Url, ct);
            var isPresent = content.Contains(_options.Keyword, StringComparison.OrdinalIgnoreCase);
            var value = isPresent ? $"present" : "absent";
            return MonitorResult.Ok(value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking keyword presence for monitor '{MonitorId}'", Id);
            return MonitorResult.Fail(ex.Message);
        }
    }
}
