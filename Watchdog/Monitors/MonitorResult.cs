namespace Watchdog.Monitors;

public sealed record MonitorResult(
    bool Success,
    string Value,
    string? ErrorMessage = null
)
{
    public static MonitorResult Ok(string value) => new(true, value);
    public static MonitorResult Fail(string error) => new(false, "", error);
}
