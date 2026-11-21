using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Watchdog.Configuration;

namespace Watchdog.State;

/// <summary>
/// Stores one text file per monitor ({monitorId}.txt) in the configured state directory.
/// A relative StateDirectory is resolved against the application folder, not the
/// working directory (a Windows service starts in System32).
/// </summary>
public class FileStateStore : IStateStore
{
    private readonly string _directory;
    private readonly ILogger<FileStateStore> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public FileStateStore(IOptions<NotificationMonitorOptions> options, ILogger<FileStateStore> logger)
    {
        var configured = string.IsNullOrWhiteSpace(options.Value.StateDirectory) ? "state" : options.Value.StateDirectory;
        _directory = Path.IsPathRooted(configured)
            ? configured
            : Path.Combine(AppContext.BaseDirectory, configured);
        _logger = logger;
        Directory.CreateDirectory(_directory);
        _logger.LogInformation("State directory: {Directory}", _directory);
    }

    public async Task<string?> GetAsync(string monitorId, CancellationToken ct = default)
    {
        var path = PathFor(monitorId);
        await _lock.WaitAsync(ct);
        try
        {
            if (!File.Exists(path)) return null;
            var value = (await File.ReadAllTextAsync(path, ct)).Trim();
            return value.Length == 0 ? null : value;
        }
        finally { _lock.Release(); }
    }

    public async Task SetAsync(string monitorId, string value, CancellationToken ct = default)
    {
        var path = PathFor(monitorId);
        var temp = path + ".tmp";
        await _lock.WaitAsync(ct);
        try
        {
            // Write then swap, so a crash mid-write never leaves a half-written state file
            await File.WriteAllTextAsync(temp, value, ct);
            File.Move(temp, path, overwrite: true);
        }
        finally { _lock.Release(); }
    }

    private string PathFor(string monitorId)
    {
        var safe = string.Concat(monitorId.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
        return Path.Combine(_directory, safe + ".txt");
    }
}
