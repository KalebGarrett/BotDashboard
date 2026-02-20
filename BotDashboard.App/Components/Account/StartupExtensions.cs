using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace BotDashboard.App;

public static class StartupExtensions
{
    public static void MapAccountServices(this IEndpointRouteBuilder app)
    {
        app
            .MapGet("/logout", async (HttpContext context, string returnUrl = "/") =>
            {
                await context.SignOutAsync(IdentityConstants.ApplicationScheme);
                context.Response.Redirect(returnUrl);
            })
            .RequireAuthorization();
    }
}