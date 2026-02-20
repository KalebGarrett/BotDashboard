using System.ComponentModel.DataAnnotations;
using BotDashboard.App.Data;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using MudBlazor;

namespace BotDashboard.App.Components.Account.Pages.Manage;

public partial class SetPassword
{
    [Inject] private UserManager<BotHarborUser> UserManager { get; set; }
    [Inject] private SignInManager<BotHarborUser> SignInManager { get; set; }
    [Inject] private IdentityUserAccessor UserAccessor { get; set; }
    [Inject] private IdentityRedirectManager RedirectManager { get; set; }

    private string? Message { get; set; }
    private BotHarborUser User { get; set; } = default!;

    [CascadingParameter] private HttpContext HttpContext { get; set; } = default!;

    [SupplyParameterFromForm] private InputModel Input { get; set; } = new();

    [Inject] private ISnackbar Snackbar { get; set; }

    protected override async Task OnInitializedAsync()
    {
        User = await UserAccessor.GetRequiredUserAsync(HttpContext);

        var hasPassword = await UserManager.HasPasswordAsync(User);
        if (hasPassword)
        {
            RedirectManager.RedirectTo("account/manage/changepassword");
        }
    }

    private async Task OnValidSubmitAsync()
    {
        var addPasswordResult = await UserManager.AddPasswordAsync(User, Input.NewPassword!);
        if (!addPasswordResult.Succeeded)
        {
            Message = $"Error: {string.Join(",", addPasswordResult.Errors.Select(error => error.Description))}";
            CreateSnackbarMessage(Message, Severity.Error);
            return;
        }

        await SignInManager.RefreshSignInAsync(User);

        RedirectManager.RedirectToCurrentPageWithStatus("Your password has been set.", HttpContext);
    }

    private void CreateSnackbarMessage(string message, Severity severity)
    {
        Snackbar.Configuration.PositionClass = Defaults.Classes.Position.BottomRight;
        Snackbar.Add(message, severity);
    }

    private sealed class InputModel
    {
        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.",
            MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string? ConfirmPassword { get; set; }
    }
}