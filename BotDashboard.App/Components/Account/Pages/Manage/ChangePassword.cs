using System.ComponentModel.DataAnnotations;
using BotDashboard.App.Data;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using MudBlazor;

namespace BotDashboard.App.Components.Account.Pages.Manage;

public partial class ChangePassword
{
    [Inject] private UserManager<BotHarborUser> UserManager { get; set; }
    [Inject] private SignInManager<BotHarborUser> SignInManager { get; set; }
    [Inject] private IdentityUserAccessor UserAccessor { get; set; }
    [Inject] private IdentityRedirectManager RedirectManager { get; set; }
    [Inject] private ILogger<ChangePassword> Logger { get; set; }

    private string? Message { get; set; }
    private BotHarborUser User { get; set; } = default!;
    private bool HasPassword { get; set; }

    [CascadingParameter] private HttpContext HttpContext { get; set; } = default!;

    [SupplyParameterFromForm] private InputModel Input { get; set; } = new();

    [Inject] private ISnackbar Snackbar { get; set; }

    protected override async Task OnInitializedAsync()
    {
        User = await UserAccessor.GetRequiredUserAsync(HttpContext);
        HasPassword = await UserManager.HasPasswordAsync(User);
        if (!HasPassword)
        {
            RedirectManager.RedirectTo("Account/Manage/SetPassword");
        }
    }

    private async Task OnValidSubmitAsync()
    {
        var changePasswordResult = await UserManager.ChangePasswordAsync(User, Input.OldPassword, Input.NewPassword);
        if (!changePasswordResult.Succeeded)
        {
            Message = $"Error: {string.Join(",", changePasswordResult.Errors.Select(error => error.Description))}";
            CreateSnackbarMessage(Message, Severity.Error);
            return;
        }

        await SignInManager.RefreshSignInAsync(User);
        Logger.LogInformation("User changed their password successfully.");
        
        RedirectManager.RedirectToCurrentPageWithStatus("Your password has been changed", HttpContext);
    }

    private void CreateSnackbarMessage(string message, Severity severity)
    {
        Snackbar.Configuration.PositionClass = Defaults.Classes.Position.BottomRight;
        Snackbar.Add(message, severity);
    }

    private sealed class InputModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Current password")]
        public string OldPassword { get; set; } = "";

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.",
            MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; } = "";

        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = "";
    }
}