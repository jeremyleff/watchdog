namespace Watchdog.State;

/// <summary>
/// Persists the last known value for each monitor so that a restart does not
/// look like a status change.
/// </summary>
public interface IStateStore
{
    Task<string?> GetAsync(string monitorId, CancellationToken ct = default);
    Task SetAsync(string monitorId, string value, CancellationToken ct = default);
}
