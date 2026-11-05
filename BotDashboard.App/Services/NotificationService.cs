using System.Text.Json;
using System.Text.Json.Serialization;
using BotDashboard.Models;

namespace BotDashboard.App.Services;

/// <summary>
/// The notification bell: rows that outlive the browser tab.
/// </summary>
/// <remarks>
/// Kept in a JSON file on the same mounted volume as the operator preferences rather than in
/// Postgres. These are notices for one operator, they are disposable, and a file needs no
/// migration. Every write is best-effort for the same reason <see cref="PreferencesService"/>
/// is: a dashboard that refuses to start because it could not file a notification would be
/// worse than one that quietly loses it.
///
/// A singleton, because the bell is shared across every circuit the operator has open.
/// </remarks>
public class NotificationService
{
    /// <summary>
    /// Enough history to answer "what happened while I was asleep", not an audit log.
    /// </summary>
    private const int MaxRows = 100;

    private readonly string _path;
    private readonly ILogger<NotificationService> _logger;
    private readonly object _gate = new();
    private readonly List<Notification> _rows = [];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public NotificationService(IConfiguration configuration, ILogger<NotificationService> logger)
    {
        _logger = logger;

        var preferences = configuration["BotHarbor:PreferencesPath"];
        _path = string.IsNullOrWhiteSpace(preferences)
            ? null
            : Path.Combine(Path.GetDirectoryName(preferences) ?? ".", "notifications.json");
    }

    /// <summary>Raised whenever the rows change, so open circuits can re-render the bell.</summary>
    public event Action Changed;

    public IReadOnlyList<Notification> Recent()
    {
        lock (_gate)
        {
            // Unread first, then newest: the thing demanding attention should not be pushed
            // off the bottom by a stream of successful clicks.
            return _rows
                .OrderByDescending(row => row.Unread)
                .ThenByDescending(row => row.CreatedAt)
                .ToList();
        }
    }

    public int UnreadCount()
    {
        lock (_gate)
        {
            return _rows.Count(row => row.Unread);
        }
    }

    /// <summary>Files a row for something that happened once.</summary>
    public void Post(string title, string message, NotificationLevel level)
    {
        Upsert($"toast:{Guid.NewGuid():N}", title, message, level);
    }

    /// <summary>
    /// Files or refreshes the row for an ongoing condition.
    /// </summary>
    /// <remarks>
    /// Re-posting the same key leaves the existing row alone rather than marking it unread
    /// again, so a container that has been down all night does not re-announce itself on every
    /// page load.
    /// </remarks>
    public void PostState(string key, string title, string message, NotificationLevel level)
    {
        Upsert(Notification.StatePrefix + key, title, message, level);
    }

    /// <summary>
    /// Replaces every state row under <paramref name="scope"/> with the ones given.
    /// </summary>
    /// <remarks>
    /// This is what makes the bell a picture of now rather than a pile of history: a condition
    /// that has cleared stops being reported because its key is simply no longer produced. The
    /// scope keeps one reconciler from withdrawing another's rows -- the container sweep must
    /// not delete the Ollama row just because it did not write one.
    /// </remarks>
    public void ReconcileState(string scope, IEnumerable<(string Key, string Title, string Message, NotificationLevel Level)> conditions)
    {
        var prefix = Notification.StatePrefix + scope;
        var wanted = conditions.ToList();
        var changed = false;

        lock (_gate)
        {
            var keep = wanted.Select(condition => prefix + condition.Key).ToHashSet();
            changed |= _rows.RemoveAll(row =>
                row.Key.StartsWith(prefix, StringComparison.Ordinal) && !keep.Contains(row.Key)) > 0;

            foreach (var condition in wanted)
            {
                changed |= UpsertLocked(prefix + condition.Key, condition.Title, condition.Message, condition.Level);
            }

            if (changed)
            {
                SaveLocked();
            }
        }

        if (changed)
        {
            Changed?.Invoke();
        }
    }

    public void MarkAllRead()
    {
        lock (_gate)
        {
            var now = DateTime.UtcNow;
            foreach (var row in _rows.Where(row => row.Unread))
            {
                row.ReadAt = now;
            }

            SaveLocked();
        }

        Changed?.Invoke();
    }

    public void ClearAll()
    {
        lock (_gate)
        {
            _rows.Clear();
            SaveLocked();
        }

        Changed?.Invoke();
    }

    /// <summary>Reads stored rows at startup. Falls back to an empty bell on any failure.</summary>
    public void Load()
    {
        if (string.IsNullOrWhiteSpace(_path) || !File.Exists(_path))
        {
            return;
        }

        try
        {
            var stored = JsonSerializer.Deserialize<List<Notification>>(File.ReadAllText(_path), JsonOptions);
            if (stored is null)
            {
                return;
            }

            lock (_gate)
            {
                _rows.Clear();
                _rows.AddRange(stored);
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Could not read notifications from {Path}; starting empty.", _path);
        }
    }

    private void Upsert(string key, string title, string message, NotificationLevel level)
    {
        bool changed;

        lock (_gate)
        {
            changed = UpsertLocked(key, title, message, level);
            if (changed)
            {
                SaveLocked();
            }
        }

        if (changed)
        {
            Changed?.Invoke();
        }
    }

    private bool UpsertLocked(string key, string title, string message, NotificationLevel level)
    {
        var existing = _rows.FirstOrDefault(row => row.Key == key);

        if (existing is not null)
        {
            if (existing.Title == title && existing.Message == message && existing.Level == level)
            {
                return false;
            }

            existing.Title = title;
            existing.Message = message;
            existing.Level = level;
            return true;
        }

        _rows.Add(new Notification
        {
            Key = key,
            Title = title,
            Message = message,
            Level = level,
            CreatedAt = DateTime.UtcNow
        });

        if (_rows.Count > MaxRows)
        {
            // Drop read rows first, oldest among them: an unread warning is the whole point of
            // the bell and must not be evicted by a run of successful clicks.
            var victim = _rows
                .OrderBy(row => row.Unread)
                .ThenBy(row => row.CreatedAt)
                .First();

            _rows.Remove(victim);
        }

        return true;
    }

    private void SaveLocked()
    {
        if (string.IsNullOrWhiteSpace(_path))
        {
            return;
        }

        try
        {
            var directory = Path.GetDirectoryName(_path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(_path, JsonSerializer.Serialize(_rows, JsonOptions));
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Could not write notifications to {Path}.", _path);
        }
    }
}
