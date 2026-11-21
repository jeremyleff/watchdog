namespace Watchdog.Configuration;

public class NotificationMonitorOptions
{
    public const string SectionName = "NotificationMonitor";

    public string StateDirectory { get; set; } = "state";
    public List<ChannelConfig> Channels { get; set; } = [];
    public List<MonitorConfig> Monitors { get; set; } = [];
}

public class ChannelConfig
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "";
    public SmtpChannelOptions? Smtp { get; set; }
    public NtfyChannelOptions? Ntfy { get; set; }
}

public class NtfyChannelOptions
{
    /// <summary>Base URL of the ntfy server, e.g. https://ntfy.sh or http://127.0.0.1:2586</summary>
    public string ServerUrl { get; set; } = "https://ntfy.sh";
    public string Topic { get; set; } = "";
    /// <summary>Access token (tk_...). Prefer AccessTokenFile so the token isn't stored in appsettings.json.</summary>
    public string? AccessToken { get; set; }
    /// <summary>Path to a file containing only the access token.</summary>
    public string? AccessTokenFile { get; set; }
    /// <summary>1 (min) to 5 (max); 3 is ntfy's default.</summary>
    public int Priority { get; set; } = 3;
    /// <summary>ntfy tags; names matching emoji shortcodes show as emoji (e.g. "bicyclist").</summary>
    public List<string> Tags { get; set; } = [];
    /// <summary>Optional URL opened when the notification is tapped.</summary>
    public string? ClickUrl { get; set; }
}

public class SmtpChannelOptions
{
    public string Host { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string FromAddress { get; set; } = "";
    public List<string> Recipients { get; set; } = [];
}

public class MonitorConfig
{
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Type { get; set; } = "";
    public int IntervalSeconds { get; set; } = 900;
    public bool NotifyOnStart { get; set; } = true;
    public List<string> ChannelIds { get; set; } = [];
    public WebContentMonitorOptions? WebContent { get; set; }
    public HttpStatusMonitorOptions? HttpStatus { get; set; }
    public KeywordPresenceMonitorOptions? KeywordPresence { get; set; }
}

public class WebContentMonitorOptions
{
    public string Url { get; set; } = "";
    public string Pattern { get; set; } = "";
    public int CaptureGroup { get; set; } = 1;
    public bool StripHtmlTags { get; set; } = true;
    public bool DecodeHtmlEntities { get; set; } = true;
    public string UserAgent { get; set; } = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.3";
}

public class HttpStatusMonitorOptions
{
    public string Url { get; set; } = "";
    public int ExpectedStatusCode { get; set; } = 200;
}

public class KeywordPresenceMonitorOptions
{
    public string Url { get; set; } = "";
    public string Keyword { get; set; } = "";
    public bool ExpectedPresent { get; set; } = true;
}
