using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Watchdog.Configuration;

namespace Watchdog.Notifications;

/// <summary>
/// Sends push notifications through an ntfy server (https://ntfy.sh or self-hosted).
/// Uses ntfy's JSON publish API so titles and messages can contain any Unicode text.
/// </summary>
public class NtfyNotificationChannel : INotificationChannel
{
    private readonly NtfyChannelOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<NtfyNotificationChannel> _logger;

    public string Id { get; }

    public NtfyNotificationChannel(
        string id,
        NtfyChannelOptions options,
        IHttpClientFactory httpClientFactory,
        ILogger<NtfyNotificationChannel> logger)
    {
        Id = id;
        _options = options;
        _httpClientFactory = httpClientFactory;
        _logger = logger;

        if (string.IsNullOrWhiteSpace(options.Topic))
            throw new InvalidOperationException($"Channel '{id}' is type Ntfy but has no Topic.");
    }

    public async Task SendAsync(NotificationMessage message, CancellationToken ct = default)
    {
        var payload = new
        {
            topic = _options.Topic,
            title = message.Subject,
            message = message.Body,
            priority = _options.Priority,
            tags = _options.Tags,
            click = string.IsNullOrWhiteSpace(_options.ClickUrl) ? null : _options.ClickUrl
        };

        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(30);

            using var request = new HttpRequestMessage(HttpMethod.Post, _options.ServerUrl.TrimEnd('/') + "/")
            {
                Content = JsonContent.Create(payload)
            };

            var token = ReadAccessToken();
            if (!string.IsNullOrEmpty(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await client.SendAsync(request, ct);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Notification sent via channel '{ChannelId}' for monitor '{MonitorId}'", Id, message.MonitorId);
            }
            else
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("ntfy rejected notification via channel '{ChannelId}' for monitor '{MonitorId}': {Status} {Body}",
                    Id, message.MonitorId, (int)response.StatusCode, body);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            _logger.LogError(ex, "Failed to send notification via channel '{ChannelId}' for monitor '{MonitorId}'", Id, message.MonitorId);
        }
    }

    /// <summary>
    /// The token is read on every send (not cached) so it can be rotated without restarting the service.
    /// Prefer AccessTokenFile so the token lives in a locked-down file instead of appsettings.json.
    /// </summary>
    private string? ReadAccessToken()
    {
        if (!string.IsNullOrWhiteSpace(_options.AccessToken))
            return _options.AccessToken.Trim();

        if (!string.IsNullOrWhiteSpace(_options.AccessTokenFile))
            return File.ReadAllText(_options.AccessTokenFile).Trim();

        return null;
    }
}
