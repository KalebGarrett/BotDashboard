using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace BotHarbor.App2.Components.Layout;

public partial class MainLayout
{
    private bool Open { get; set; } = true;
    private ErrorBoundary ErrorBoundary { get; set; } = new();
    
    [Inject] private NavigationManager NavigationManager { get; set; }

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