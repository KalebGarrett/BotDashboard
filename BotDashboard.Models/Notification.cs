namespace BotDashboard.Models;

/// <summary>How loud a notification is.</summary>
public enum NotificationLevel
{
    Info,
    Success,
    Warning,
    Error
}

/// <summary>
/// One row in the notification bell.
/// </summary>
/// <remarks>
/// A toast only lands if a browser happens to be open, so anything worth saying is also filed
/// here. Otherwise a container that dies at 2am announces itself to an empty room.
/// </remarks>
public class Notification
{
    /// <summary>
    /// Identity for the thing being reported, not for this row.
    /// </summary>
    /// <remarks>
    /// Rows about a condition use a stable key such as <c>state:container:yt-cipher</c>, so a
    /// container that has been down for an hour is one row rather than sixty. Rows about an
    /// event that happened once use a unique <c>toast:{guid}</c>.
    /// </remarks>
    public string Key { get; set; }

    public string Title { get; set; }

    public string Message { get; set; }

    public NotificationLevel Level { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ReadAt { get; set; }

    public bool Unread => ReadAt is null;

    /// <summary>Rows the reconciler owns, and will withdraw once the condition clears.</summary>
    public const string StatePrefix = "state:";
}
