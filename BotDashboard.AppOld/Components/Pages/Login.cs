using BotDashboard.App.Services.Authentication;
using BotDashboard.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace BotDashboard.App.Components.Pages;

public partial class Login
{
    private LoginDTO LoginDTO { get; set; } // Idk if this is an appropriate name
    
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; }
    
    private bool Success { get; set; }
    
    public string userIdentifier = string.Empty;

    private void SignIn()
    {
        ((AuthenticationService)AuthenticationStateProvider)
            .AuthenticateUser(userIdentifier);
    }
    
    private async Task HandleLogin()
    {
        
    }
    
    private void OnValidSubmit()
    {
        Success = true;
    }
}