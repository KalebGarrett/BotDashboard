using System.Text.Json;

namespace BotDashboard.App.Services;

/// <summary>
/// Operator preferences that outlive a restart.
/// </summary>
/// <remarks>
/// Kept in a small JSON file on a mounted volume rather than in the database. These are
/// settings for the one operator rather than per-user data, and a file needs no migration and
/// no round trip to Postgres on every page load.
///
/// Every operation is best-effort: a dashboard must still start if the file is missing,
/// unreadable or on a read-only mount. A lost preference is an annoyance; refusing to boot
/// over one is not acceptable.
/// </remarks>
public class PreferencesService
{
    private readonly string _path;
    private readonly ILogger<PreferencesService> _logger;
    private readonly object _gate = new();

    public PreferencesService(IConfiguration configuration, ILogger<PreferencesService> logger)
    {
        _logger = logger;
        _path = configuration["BotHarbor:PreferencesPath"];
    }

    public bool AutoLoadData { get; private set; }

    /// <summary>Reads the stored preferences. Falls back to defaults on any failure.</summary>
    public void Load()
    {
        if (string.IsNullOrWhiteSpace(_path) || !File.Exists(_path))
        {
            return;
        }

        try
        {
            var stored = JsonSerializer.Deserialize<StoredPreferences>(File.ReadAllText(_path));
            AutoLoadData = stored?.AutoLoadData ?? false;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Could not read preferences from {Path}; using defaults.", _path);
        }
    }

    public void SetAutoLoadData(bool enabled)
    {
        AutoLoadData = enabled;
        Save();
    }

    private void Save()
    {
        if (string.IsNullOrWhiteSpace(_path))
        {
            _logger.LogWarning("BotHarbor:PreferencesPath is not set, so preferences will not survive a restart.");
            return;
        }

        try
        {
            lock (_gate)
            {
                var directory = Path.GetDirectoryName(_path);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(_path,
                    JsonSerializer.Serialize(new StoredPreferences { AutoLoadData = AutoLoadData }));
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Could not write preferences to {Path}.", _path);
        }
    }

    private sealed class StoredPreferences
    {
        public bool AutoLoadData { get; set; }
    }
}
