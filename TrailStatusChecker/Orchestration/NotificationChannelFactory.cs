using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Watchdog.Configuration;
using Watchdog.Notifications;

namespace Watchdog.Orchestration;

public class NotificationChannelFactory
{
    private readonly IReadOnlyDictionary<string, INotificationChannel> _channels;

    public NotificationChannelFactory(
        IOptions<NotificationMonitorOptions> options,
        ILoggerFactory loggerFactory)
    {
        var channels = new Dictionary<string, INotificationChannel>();
        foreach (var config in options.Value.Channels)
        {
            INotificationChannel channel = config.Type switch
            {
                "Smtp" when config.Smtp is not null => new SmtpNotificationChannel(
                    config.Id,
                    config.Smtp,
                    loggerFactory.CreateLogger<SmtpNotificationChannel>()),
                "Smtp" => throw new InvalidOperationException($"Channel '{config.Id}' is type Smtp but has no Smtp configuration."),
                _ => throw new InvalidOperationException($"Unknown channel type '{config.Type}' for channel '{config.Id}'.")
            };
            channels[config.Id] = channel;
        }
        _channels = channels;
    }

    public INotificationChannel? GetById(string id) =>
        _channels.TryGetValue(id, out var channel) ? channel : null;
}
