using BotDashboard.App.Services;
using DigitalOcean.API.Models.Responses;
using Microsoft.AspNetCore.Components;

namespace BotDashboard.App.Components.Pages;

public partial class ManageDroplet
{
    [Inject] private DigitalOceanService DigitalOceanService { get; set; }

    private Droplet Droplet { get; set; } = new();

    private bool PowerSwitch { get; set; }
    
    private bool Locked { get; set; }

    private string ErrorMessage { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await GetDropletInfo();
        PowerSwitch = Droplet.Status == "active";
    }

    private async Task GetDropletInfo()
    {
        Locked = true;
        await InvokeAsync(StateHasChanged);
        await Task.Yield();
        
        try
        {
            Droplet = await DigitalOceanService.GetDropletInfo();
            ErrorMessage = null;
        }
        catch (Exception exception)
        {
            // Keep the circuit alive so the page can say what is wrong. An exception escaping
            // here reaches the renderer and the user only sees "Oops... something went wrong".
            Droplet = new Droplet();
            ErrorMessage = exception.Message;
        }

        PowerSwitch = Droplet.Status == "active";
        
        Locked = false;
        await InvokeAsync(StateHasChanged);
    }

    private async Task OnPowerSwitchChanged()
    {
        Locked = true;
        await InvokeAsync(StateHasChanged);
        await Task.Yield();
        
        if (Droplet.Status == "active")
        {
            await DigitalOceanService.PowerOffDroplet();
        }
        else
        {
            await DigitalOceanService.PowerOnDroplet();
        }

        Locked = false;
        await GetDropletInfo();
        await InvokeAsync(StateHasChanged);
    }

    private async Task RebootDroplet()
    {
        Locked = true;
        await InvokeAsync(StateHasChanged);
        await Task.Yield();
        
        await DigitalOceanService.RebootDroplet();
        
        Locked = false;
        await GetDropletInfo();
        await InvokeAsync(StateHasChanged);
    }
}