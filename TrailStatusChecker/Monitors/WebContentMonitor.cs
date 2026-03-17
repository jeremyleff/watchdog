using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using Microsoft.Extensions.Logging;
using Watchdog.Configuration;

namespace Watchdog.Monitors;

public class WebContentMonitor : IMonitor
{
    private readonly WebContentMonitorOptions _options;
    private readonly HttpClient _httpClient;
    private readonly ILogger<WebContentMonitor> _logger;

    public string Id { get; }
    public string DisplayName { get; }
    public TimeSpan Interval { get; }
    public bool NotifyOnStart { get; }
    public IReadOnlyList<string> NotificationChannelIds { get; }

    public WebContentMonitor(MonitorConfig config, IHttpClientFactory httpClientFactory, ILogger<WebContentMonitor> logger)
    {
        Id = config.Id;
        DisplayName = config.DisplayName;
        Interval = TimeSpan.FromSeconds(config.IntervalSeconds);
        NotifyOnStart = config.NotifyOnStart;
        NotificationChannelIds = config.ChannelIds.AsReadOnly();
        _options = config.WebContent ?? throw new ArgumentException($"Monitor '{config.Id}' is missing WebContent configuration.");
        _logger = logger;

        var handler = new HttpClientHandler { CookieContainer = new CookieContainer() };
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(_options.UserAgent);
        _httpClient.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        _httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.5");
        _httpClient.DefaultRequestHeaders.Referrer = new Uri("https://www.google.com/");
        _httpClient.DefaultRequestHeaders.Connection.ParseAdd("keep-alive");
        _httpClient.DefaultRequestHeaders.Add("DNT", "1");
        _httpClient.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
    }

    public async Task<MonitorResult> CheckAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetStringAsync(_options.Url, ct);
            var match = Regex.Match(response, _options.Pattern, RegexOptions.Singleline);

            if (!match.Success || match.Groups.Count <= _options.CaptureGroup)
            {
                _logger.LogWarning("Pattern did not match for monitor '{MonitorId}'. URL: {Url}", Id, _options.Url);
                return MonitorResult.Fail("Pattern did not match page content.");
            }

            var raw = match.Groups[_options.CaptureGroup].Value;

            if (_options.StripHtmlTags)
                raw = Regex.Replace(raw, "<.*?>", "").Trim();

            if (_options.DecodeHtmlEntities)
                raw = HttpUtility.HtmlDecode(raw);

            return MonitorResult.Ok(raw.Trim());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching content for monitor '{MonitorId}'", Id);
            return MonitorResult.Fail(ex.Message);
        }
    }
}
