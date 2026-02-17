using BotDashboard.App.Data;
using Microsoft.AspNetCore.Identity;

namespace BotDashboard.App.Components.Account;

internal sealed class IdentityUserAccessor(
    UserManager<BotHarborUser> userManager,
    IdentityRedirectManager redirectManager)
{
    public async Task<BotHarborUser> GetRequiredUserAsync(HttpContext context)
    {
        var user = await userManager.GetUserAsync(context.User);

        if (user is null)
        {
            redirectManager.RedirectToWithStatus("Account/InvalidUser",
                $"Error: Unable to load user with ID '{userManager.GetUserId(context.User)}'.", context);
        }

        return user;
    }
}