using System.Globalization;
using BotDashboard.App.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace BotDashboard.App.Components.Pages;

public partial class Stats
{
    [Inject] private UbuntuService UbuntuService { get; set; }

    private static double MemoryUsagePercentage { get; set; }
    private double[] MemoryUsageData { get; set; }
    private string[] MemoryUsageLabels { get; set; } = { "Used Memory", "Total Memory" };
    private string MemoryFetchTime { get; set; }

    private static double CpuUsagePercentage { get; set; }
    private double[] CpuUsageData { get; set; }
    private string[] CpuUsageLabels { get; set; } = { "Used CPU", "Total CPU" };
    private string CpuFetchTime { get; set; }

    private double DiskUsed { get; set; }
    private static double DiskUsedPercentage { get; set; }
    private double FreeDisk { get; set; }
    private static double FreeDiskPercentage { get; set; }
    private string DiskFetchTime { get; set; }
    
    private bool Locked { get; set; }
    
    private List<SankeyChartNode> Nodes { get; } =
    [
        new("Droplet", 0),
        
        new("/media", 1),
        new("/snap", 1),
        new("/mnt", 1),
        new("/sys", 1),
        new("/var", 1),
        new("/dev", 1),
        new("/srv", 1),
        new("/opt", 1),
        new("/root", 1),
        new("/tmp", 1),
        new("/etc", 1),
        new("/lost+found", 1),
        new("/boot", 1),
        new("/usr", 1),
        new("/run", 1),
    ];

    private List<SankeyChartEdge> Edges { get; } =
    [
        new("Droplet", "/media", 1),
        new("Droplet", "/snap", 9),
        new("Droplet", "/mnt", 1),
        new("Droplet", "/sys", 1),
        new("Droplet", "/var", 57),
        new("Droplet", "/dev", 1),
        new("Droplet", "/srv", 1),
        new("Droplet", "/opt", 1),
        new("Droplet", "/tmp", 1),
        new("Droplet", "/etc", 1),
        new("Droplet", "/lost+found", 1),
        new("Droplet", "/usr", 22),
        new("Droplet", "/boot", 2),
        new("Droplet", "/run", 1),
    ];

    protected override async Task OnInitializedAsync()
    {
        if (Switch.OnInitialized == false)
        {
            MemoryUsagePercentage = 0;
            CpuUsagePercentage = 0;
            
            DiskUsed = 0;
            DiskUsedPercentage = 0;
            
            FreeDisk = 0;
            FreeDiskPercentage = 0;
            
            await Task.CompletedTask;
            return;
        }

        await ListMemoryUsagePercentage(showSnackbar: false);
        await ListCpuUsagePercentage(showSnackbar: false);
        await ListDiskUsage(showSnackbar: false);
    }

    private async Task ListMemoryUsagePercentage(bool showSnackbar = true)
    {
        Locked = true;
        await InvokeAsync(StateHasChanged);
        await Task.Yield();
        
        MemoryUsagePercentage = UbuntuService.MemoryUsagePercentage();
        MemoryUsageData = [MemoryUsagePercentage, 100 - MemoryUsagePercentage];
        MemoryFetchTime = DateTime.UtcNow.ToLocalTime().ToString(CultureInfo.CurrentCulture);

        if (showSnackbar)
        {
            CreateSnackbarMessage("Successfully listed memory usage!", Severity.Success);
        }
        
        Locked = false;
        await InvokeAsync(StateHasChanged);
    }

    private async Task ListCpuUsagePercentage(bool showSnackbar = true)
    {
        Locked = true;
        await InvokeAsync(StateHasChanged);
        await Task.Yield();
        
        CpuUsagePercentage = UbuntuService.CpuUsagePercentage();
        CpuUsageData = [CpuUsagePercentage, 100 - CpuUsagePercentage];
        CpuFetchTime = DateTime.UtcNow.ToLocalTime().ToString(CultureInfo.CurrentCulture);

        if (showSnackbar)
        {
            CreateSnackbarMessage("Successfully listed CPU usage!", Severity.Success);
        }
        
        Locked = false;
        await InvokeAsync(StateHasChanged);
    }

    private async Task ListDiskUsage(bool showSnackbar = true)
    {
        Locked = true;
        await InvokeAsync(StateHasChanged);
        await Task.Yield();
        
        DiskUsed = UbuntuService.DiskUsage();
        DiskUsedPercentage = (DiskUsed / 25) * 100;

        FreeDisk = 25 - DiskUsed;
        FreeDiskPercentage = 100 - DiskUsedPercentage;
      
        DiskFetchTime = DateTime.UtcNow.ToLocalTime().ToString(CultureInfo.CurrentCulture);

        if (showSnackbar)
        {
            CreateSnackbarMessage("Successfully listed Disk usage!", Severity.Success);
        }
        
        Locked = false;
        await InvokeAsync(StateHasChanged);
    }

    private void CreateSnackbarMessage(string message, Severity severity)
    {
        Snackbar.Configuration.PositionClass = Defaults.Classes.Position.BottomRight;
        Snackbar.Add(message, severity);
    }
}