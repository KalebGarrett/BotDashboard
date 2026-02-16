using Microsoft.AspNetCore.Identity;
using BotHarbor.App2.Data;

namespace BotHarbor.App2.Components.Account;

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