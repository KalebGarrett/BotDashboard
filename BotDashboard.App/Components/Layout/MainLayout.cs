using BotDashboard.App.Services;
using BotDashboard.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace BotDashboard.App.Components.Layout;

public partial class MainLayout : IDisposable
{
    /// <summary>Desktop drawer: open wide, or collapsed to the icon rail.</summary>
    private bool Open { get; set; } = true;

    /// <summary>
    /// Phone drawer: on screen, or not there at all.
    /// </summary>
    /// <remarks>
    /// Separate from <see cref="Open"/> on purpose. They mean different things -- one picks
    /// between two widths, the other between showing and hiding -- and sharing a field made a
    /// collapsed rail on the desktop reopen as a full overlay the next time the phone layout
    /// loaded. It starts closed, because on a phone the content is the point.
    /// </remarks>
    private bool MobileOpen { get; set; }

    private ErrorBoundary ErrorBoundary { get; set; } = new();

    [Inject] private NavigationManager NavigationManager { get; set; }

    [Inject] private NotificationService NotificationService { get; set; }

    private IReadOnlyList<Notification> Notifications { get; set; } = [];

    private int UnreadCount { get; set; }

    protected override void OnInitialized()
    {
        // The service is a singleton shared by every circuit, so a row filed by a background
        // action or another tab has to push itself here rather than wait to be polled.
        NotificationService.Changed += OnNotificationsChanged;
        ReadNotifications();
    }

    private void OnNotificationsChanged()
    {
        InvokeAsync(() =>
        {
            ReadNotifications();
            StateHasChanged();
        });
    }

    private void ReadNotifications()
    {
        Notifications = NotificationService.Recent();
        UnreadCount = NotificationService.UnreadCount();
    }

    private void MarkAllRead() => NotificationService.MarkAllRead();

    private void ClearNotifications() => NotificationService.ClearAll();

    private static string LevelModifier(NotificationLevel level) => level switch
    {
        NotificationLevel.Success => "bh-bell__dot--success",
        NotificationLevel.Warning => "bh-bell__dot--warning",
        NotificationLevel.Error => "bh-bell__dot--error",
        _ => "bh-bell__dot--info"
    };

    /// <summary>Relative age, because "4m ago" reads faster than a timestamp on a feed.</summary>
    private static string Ago(DateTime createdAtUtc)
    {
        var elapsed = DateTime.UtcNow - createdAtUtc;

        if (elapsed < TimeSpan.FromMinutes(1))
        {
            return "just now";
        }

        if (elapsed < TimeSpan.FromHours(1))
        {
            return $"{(int)elapsed.TotalMinutes}m ago";
        }

        if (elapsed < TimeSpan.FromDays(1))
        {
            return $"{(int)elapsed.TotalHours}h ago";
        }

        return $"{(int)elapsed.TotalDays}d ago";
    }

    public void Dispose()
    {
        NotificationService.Changed -= OnNotificationsChanged;
    }

    private RenderFragment GetErrorContent()
    {
        return _ =>
        {
            Snackbar.Configuration.PositionClass = Defaults.Classes.Position.BottomRight;
            Snackbar.Configuration.PositionClass = Defaults.Classes.Position.BottomRight;
            Snackbar.Add("Oops… something went wrong. Please try again.", Severity.Error);
        };
    }
    
    protected override void OnParametersSet()
    {
        ErrorBoundary.Recover();
    }
    
    private void ToggleDrawer()
    {
        Open = !Open;
    }

    /// <summary>The arrow at the foot of the phone drawer dismisses it rather than narrowing it.</summary>
    private void CloseMobileDrawer()
    {
        MobileOpen = false;
    }

    private void Logout()
    {
        NavigationManager.NavigateTo("/logout", true);
    }
    
    private void NavigateToLogin()
    {
        NavigationManager.NavigateTo("/login");
    }
}