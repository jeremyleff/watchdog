using Microsoft.Extensions.Logging;
using Watchdog.Configuration;

namespace Watchdog.Monitors;

public class HttpStatusMonitor : IMonitor
{
    private readonly HttpStatusMonitorOptions _options;
    private readonly HttpClient _httpClient;
    private readonly ILogger<HttpStatusMonitor> _logger;

    public string Id { get; }
    public string DisplayName { get; }
    public TimeSpan Interval { get; }
    public bool NotifyOnStart { get; }
    public IReadOnlyList<string> NotificationChannelIds { get; }

    public HttpStatusMonitor(MonitorConfig config, IHttpClientFactory httpClientFactory, ILogger<HttpStatusMonitor> logger)
    {
        Id = config.Id;
        DisplayName = config.DisplayName;
        Interval = TimeSpan.FromSeconds(config.IntervalSeconds);
        NotifyOnStart = config.NotifyOnStart;
        NotificationChannelIds = config.ChannelIds.AsReadOnly();
        _options = config.HttpStatus ?? throw new ArgumentException($"Monitor '{config.Id}' is missing HttpStatus configuration.");
        _httpClient = httpClientFactory.CreateClient();
        _logger = logger;
    }

    public async Task<MonitorResult> CheckAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(_options.Url, HttpCompletionOption.ResponseHeadersRead, ct);
            var statusCode = (int)response.StatusCode;
            var isExpected = statusCode == _options.ExpectedStatusCode;
            var value = isExpected ? $"UP ({statusCode})" : $"DOWN ({statusCode}, expected {_options.ExpectedStatusCode})";
            return MonitorResult.Ok(value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking HTTP status for monitor '{MonitorId}'", Id);
            return MonitorResult.Fail(ex.Message);
        }
    }
}
