using BotDashboard.App.Services;
using BotDashboard.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace BotDashboard.App.Components.Layout;

public partial class MainLayout : IDisposable
{
    /// <summary>
    /// Whether the drawer is wide or collapsed.
    /// </summary>
    /// <remarks>
    /// One field for both layouts. Collapsed means an icon rail on a desktop and nothing at
    /// all on a phone, but that difference is CSS -- the state is the same question either
    /// way, and keeping it in one place is what lets the drawer render server-side instead of
    /// waiting on a breakpoint from the browser.
    /// </remarks>
    private bool Open { get; set; } = true;

    /// <summary>
    /// True until the browser has reported how wide it is.
    /// </summary>
    /// <remarks>
    /// The drawer renders server-side, where there is no window to measure, so it starts open:
    /// right for a desktop, wrong for a phone, where it would cover the page. While this is
    /// set, app.css holds the drawer at zero width below the Md breakpoint, so a phone never
    /// sees the open state flash past before the first measurement arrives.
    /// </remarks>
    private bool AwaitingViewport { get; set; } = true;

    /// <summary>Whether the last reported breakpoint was a phone-sized one.</summary>
    private bool? Narrow { get; set; }

    private readonly Guid _viewportSubscription = Guid.NewGuid();

    private ErrorBoundary ErrorBoundary { get; set; } = new();

    [Inject] private NavigationManager NavigationManager { get; set; }

    [Inject] private IBrowserViewportService BrowserViewport { get; set; }

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

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        // fireImmediately, so the current breakpoint arrives without waiting for a resize that
        // may never come -- a phone that is never rotated would otherwise keep the server's
        // guess forever.
        await BrowserViewport.SubscribeAsync(_viewportSubscription, OnViewportChanged,
            fireImmediately: true);
    }

    /// <summary>
    /// Opens or collapses the drawer to suit the screen it is now on.
    /// </summary>
    /// <remarks>
    /// Only acts when the screen crosses between phone-sized and not. Reapplying it on every
    /// resize event would reopen a rail the operator had just collapsed, every time the window
    /// moved a pixel -- their choice wins inside a size class, this only sets the default on
    /// arriving in one.
    /// </remarks>
    private async Task OnViewportChanged(BrowserViewportEventArgs args)
    {
        // Md starts at 960px, which is the boundary app.css uses to take the rail to nothing.
        var narrow = args.Breakpoint is Breakpoint.Xs or Breakpoint.Sm;

        if (Narrow == narrow)
        {
            return;
        }

        Narrow = narrow;
        Open = !narrow;
        AwaitingViewport = false;

        await InvokeAsync(StateHasChanged);
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

        // Fire and forget: Dispose is synchronous, and leaving the subscription behind would
        // have the service calling back into a disposed circuit on the next resize.
        _ = BrowserViewport.UnsubscribeAsync(_viewportSubscription);
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


    private void Logout()
    {
        NavigationManager.NavigateTo("/logout", true);
    }
    
    private void NavigateToLogin()
    {
        NavigationManager.NavigateTo("/login");
    }
}