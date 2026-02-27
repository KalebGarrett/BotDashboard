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
        
        Droplet = await DigitalOceanService.GetDropletInfo();
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