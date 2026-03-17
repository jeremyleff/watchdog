using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Watchdog.Configuration;

namespace Watchdog.Notifications;

public class SmtpNotificationChannel : INotificationChannel
{
    private readonly SmtpChannelOptions _options;
    private readonly ILogger<SmtpNotificationChannel> _logger;

    public string Id { get; }

    public SmtpNotificationChannel(string id, SmtpChannelOptions options, ILogger<SmtpNotificationChannel> logger)
    {
        Id = id;
        _options = options;
        _logger = logger;
    }

    public Task SendAsync(NotificationMessage message, CancellationToken ct = default)
    {
        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            Credentials = new NetworkCredential(_options.Username, _options.Password),
            EnableSsl = _options.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false
        };

        var mail = new MailMessage
        {
            From = new MailAddress(_options.FromAddress),
            Subject = message.Subject,
            Body = message.Body,
            IsBodyHtml = false
        };

        foreach (var recipient in _options.Recipients)
            mail.Bcc.Add(recipient.Trim());

        try
        {
            client.Send(mail);
            _logger.LogInformation("Notification sent via channel '{ChannelId}' for monitor '{MonitorId}'", Id, message.MonitorId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification via channel '{ChannelId}' for monitor '{MonitorId}'", Id, message.MonitorId);
        }

        return Task.CompletedTask;
    }
}
