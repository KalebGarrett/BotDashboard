using System.ComponentModel.DataAnnotations;
using BotDashboard.App.Data;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using MudBlazor;

namespace BotDashboard.App.Components.Account.Pages.Manage;

public partial class ChangeEmail
{
    [Inject] private UserManager<BotHarborUser> UserManager { get; set; }
    [Inject] private SignInManager<BotHarborUser> SignInManager { get; set; }
    [Inject] private IdentityUserAccessor UserAccessor { get; set; }
    [Inject] private IdentityRedirectManager RedirectManager { get; set; }
    [Inject] private ILogger<ChangeEmail> Logger { get; set; }
    [Inject] private ISnackbar Snackbar { get; set; }

    private BotHarborUser User { get; set; } = default!;
    private string CurrentEmail { get; set; }

    [CascadingParameter] private HttpContext HttpContext { get; set; } = default!;

    [SupplyParameterFromForm] private InputModel Input { get; set; } = new();

    protected override async Task OnInitializedAsync()
    {
        User = await UserAccessor.GetRequiredUserAsync(HttpContext);
        CurrentEmail = await UserManager.GetEmailAsync(User);
    }

    private async Task OnValidSubmitAsync()
    {
        // Require the password. Without it, anyone who reaches a logged-in browser could move
        // the account to an address they control.
        if (!await UserManager.CheckPasswordAsync(User, Input.Password))
        {
            CreateSnackbarMessage("That password is not correct.", Severity.Error);
            return;
        }

        if (string.Equals(Input.NewEmail, CurrentEmail, StringComparison.OrdinalIgnoreCase))
        {
            CreateSnackbarMessage("That is already your email address.", Severity.Info);
            return;
        }

        // Identity's normal flow mails a confirmation link. This deployment has no mail sender,
        // so generate the token and redeem it immediately. ChangeEmailAsync is still the right
        // call rather than SetEmailAsync, because it leaves EmailConfirmed true -- and with
        // RequireConfirmedAccount on, SetEmailAsync would clear that flag and lock the account
        // out with no way to confirm it again.
        var token = await UserManager.GenerateChangeEmailTokenAsync(User, Input.NewEmail);
        var changeEmailResult = await UserManager.ChangeEmailAsync(User, Input.NewEmail, token);

        if (!changeEmailResult.Succeeded)
        {
            CreateSnackbarMessage(
                $"Error: {string.Join(", ", changeEmailResult.Errors.Select(error => error.Description))}",
                Severity.Error);
            return;
        }

        // The account signs in by email, so UserName has to follow it or the new address will
        // not work at the login form.
        var changeUserNameResult = await UserManager.SetUserNameAsync(User, Input.NewEmail);

        if (!changeUserNameResult.Succeeded)
        {
            CreateSnackbarMessage(
                $"Email changed, but the username did not follow: "
                + $"{string.Join(", ", changeUserNameResult.Errors.Select(error => error.Description))}. "
                + "Sign in with the old address and try again.",
                Severity.Warning);
            return;
        }

        // Both calls rotate the security stamp, which invalidates the current cookie.
        await SignInManager.RefreshSignInAsync(User);

        Logger.LogInformation("Sign-in email changed.");
        RedirectManager.RedirectToCurrentPageWithStatus("Your email has been changed", HttpContext);
    }

    private void CreateSnackbarMessage(string message, Severity severity)
    {
        Snackbar.Configuration.PositionClass = Defaults.Classes.Position.BottomRight;
        Snackbar.Add(message, severity);
    }

    private sealed class InputModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "New email")]
        public string NewEmail { get; set; } = "";

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Current password")]
        public string Password { get; set; } = "";
    }
}
