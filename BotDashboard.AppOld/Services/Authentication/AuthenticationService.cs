using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace BotDashboard.App.Services.Authentication;

public class AuthenticationService : AuthenticationStateProvider
{
    private readonly NavigationManager NavigationManager;

    public AuthenticationService(NavigationManager navigationManager)
    {
        NavigationManager = navigationManager;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var identity = new ClaimsIdentity();
        var user = new ClaimsPrincipal(identity);

        return Task.FromResult(new AuthenticationState(user));
    }
    
    public void AuthenticateUser(string userIdentifier)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Name, userIdentifier),
        ], "Custom Authentication"); // Must retrieve the user's and add it to navbar

        var user = new ClaimsPrincipal(identity);
        
        NotifyAuthenticationStateChanged(
            Task.FromResult(new AuthenticationState(user)));
    }
}