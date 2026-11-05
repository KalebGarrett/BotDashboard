using BotDashboard.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace BotDashboard.App.Services;

/// <summary>
/// MudBlazor's snackbar, with every message also filed in the notification bell.
/// </summary>
/// <remarks>
/// A decorator rather than a change to every caller. The pages already raise a snackbar for
/// each action they take, so wrapping the one service turns all of them into bell rows at a
/// stroke, and a page added later gets the behaviour without knowing this exists.
///
/// Only the string and <see cref="MarkupString"/> overloads are filed. The ones taking a
/// component or a render fragment have no text to store, so they pass straight through.
/// </remarks>
public class BellSyncedSnackbar : ISnackbar
{
    private readonly ISnackbar _inner;
    private readonly NotificationService _notifications;

    public BellSyncedSnackbar(ISnackbar inner, NotificationService notifications)
    {
        _inner = inner;
        _notifications = notifications;
    }

    public SnackbarConfiguration Configuration => _inner.Configuration;

    public IEnumerable<Snackbar> ShownSnackbars => _inner.ShownSnackbars;

    public event Action OnSnackbarsUpdated
    {
        add => _inner.OnSnackbarsUpdated += value;
        remove => _inner.OnSnackbarsUpdated -= value;
    }

    public Snackbar Add(string message, Severity severity = Severity.Normal,
        Action<SnackbarOptions> configure = null, string key = "")
    {
        File(message, severity);
        return _inner.Add(message, severity, configure, key);
    }

    public Snackbar Add(MarkupString message, Severity severity = Severity.Normal,
        Action<SnackbarOptions> configure = null, string key = "")
    {
        File(message.Value, severity);
        return _inner.Add(message, severity, configure, key);
    }

    public Snackbar Add<T>(Dictionary<string, object> componentParameters = null,
        Severity severity = Severity.Normal, Action<SnackbarOptions> configure = null,
        string key = "") where T : IComponent
    {
        return _inner.Add<T>(componentParameters, severity, configure, key);
    }

    public Snackbar Add(RenderFragment message, Severity severity = Severity.Normal,
        Action<SnackbarOptions> configure = null, string key = "")
    {
        return _inner.Add(message, severity, configure, key);
    }

    public void Clear() => _inner.Clear();

    public void Remove(Snackbar snackbar) => _inner.Remove(snackbar);

    public void RemoveByKey(string key) => _inner.RemoveByKey(key);

    public void Dispose() => _inner.Dispose();

    private void File(string message, Severity severity)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        // Filing must never be the reason an action appears to fail: the snackbar below is the
        // user-visible part, and a read-only volume should cost a bell row, not the message.
        try
        {
            _notifications.Post("Bot Harbor", message, Translate(severity));
        }
        catch
        {
            // NotificationService already logs its own failures.
        }
    }

    private static NotificationLevel Translate(Severity severity) => severity switch
    {
        Severity.Success => NotificationLevel.Success,
        Severity.Warning => NotificationLevel.Warning,
        Severity.Error => NotificationLevel.Error,
        _ => NotificationLevel.Info
    };
}
