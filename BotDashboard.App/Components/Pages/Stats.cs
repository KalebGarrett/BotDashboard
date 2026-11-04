using System.Globalization;
using BotDashboard.App.Services;
using BotDashboard.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace BotDashboard.App.Components.Pages;

public partial class Stats
{
    [Inject] private UbuntuService UbuntuService { get; set; }

    private static double MemoryUsagePercentage { get; set; }
    private double[] MemoryUsageData { get; set; } = [0, 100];
    private string[] MemoryUsageLabels { get; set; } = ["Used Memory", "Total Memory"];
    private string MemoryFetchTime { get; set; }

    private static double CpuUsagePercentage { get; set; }
    private double[] CpuUsageData { get; set; } = [0, 100];
    private string[] CpuUsageLabels { get; set; } = ["Used CPU", "Total CPU"];
    private string CpuFetchTime { get; set; }

    private List<SankeyChartNode> Nodes { get; } =
    [
        new("Cortex", 0),

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
        new("Cortex", "/media", 1),
        new("Cortex", "/snap", 9),
        new("Cortex", "/mnt", 1),
        new("Cortex", "/sys", 1),
        new("Cortex", "/var", 57),
        new("Cortex", "/dev", 1),
        new("Cortex", "/srv", 1),
        new("Cortex", "/opt", 1),
        new("Cortex", "/tmp", 1),
        new("Cortex", "/etc", 1),
        new("Cortex", "/lost+found", 1),
        new("Cortex", "/usr", 22),
        new("Cortex", "/boot", 2),
        new("Cortex", "/run", 1),
    ];

    private double DiskUsed { get; set; }
    private double DiskTotal { get; set; }
    private static double DiskUsedPercentage { get; set; }
    private double FreeDisk { get; set; }
    private static double FreeDiskPercentage { get; set; }
    private string DiskFetchTime { get; set; }

    private string Uptime { get; set; }
    private DateTime BootTime { get; set; }
    private string UptimeFetchTime { get; set; }

    private AxisChartOptions AxisChartOptions { get; set; } = new();
    private int Index { get; set; } = -1;

    private List<ChartSeries> SshLoginsSeries { get; set; } =
    [
        new() { Name = "SSH Logins", Data = [0, 0, 0, 0, 0, 0, 0] }
    ];

    private string[] SshLoginsXAxisLabels { get; set; } = ["Sun", "Mon", "Tues", "Wed", "Thurs", "Fri", "Sat"];
    private List<SshLogin> SshLogins { get; set; } = new();
    private string SshLoginsFetchTime { get; set; }

    private bool Locked { get; set; }

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
        await ListUptime(showSnackbar: false);
        await ListSshLogins(showSnackbar: false);
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
        DiskTotal = UbuntuService.DiskTotal();

        // Read the real size rather than assuming one. This used to divide by a hardcoded 25,
        // the droplet's disk; against Cortex's ~107 GiB root that reported several hundred
        // percent used and a negative amount free.
        DiskUsedPercentage = DiskTotal > 0 ? (DiskUsed / DiskTotal) * 100 : 0;

        FreeDisk = Math.Max(DiskTotal - DiskUsed, 0);
        FreeDiskPercentage = 100 - DiskUsedPercentage;

        DiskFetchTime = DateTime.UtcNow.ToLocalTime().ToString(CultureInfo.CurrentCulture);

        if (showSnackbar)
        {
            CreateSnackbarMessage("Successfully listed Disk usage!", Severity.Success);
        }

        Locked = false;
        await InvokeAsync(StateHasChanged);
    }

    private async Task ListUptime(bool showSnackbar = true)
    {
        Locked = true;
        await InvokeAsync(StateHasChanged);
        await Task.Yield();

        Uptime = UbuntuService.Uptime().Replace("up", "");

        var rawTime = UbuntuService.BootTime().Replace("system boot", "");
        BootTime = Convert.ToDateTime(rawTime);

        UptimeFetchTime = DateTime.UtcNow.ToLocalTime().ToString(CultureInfo.CurrentCulture);

        if (showSnackbar)
        {
            CreateSnackbarMessage("Successfully listed Uptime!", Severity.Success);
        }

        Locked = false;
        await InvokeAsync(StateHasChanged);
    }

    private async Task ListSshLogins(bool showSnackbar = true)
    {
        Locked = true;
        await InvokeAsync(StateHasChanged);
        await Task.Yield();

        SshLogins = UbuntuService.SshLogins();
        
        var weeklyLoginCount = new List<double>();
        foreach (var login in SshLogins)
        {
            weeklyLoginCount.Add(Convert.ToDouble(login.Count));
        }

        SshLoginsSeries =
        [
            new()
            {
                Name = "SSH Logins",
                Data = weeklyLoginCount.ToArray()
            }
        ];
        
        SshLoginsFetchTime = DateTime.UtcNow.ToLocalTime().ToString(CultureInfo.CurrentCulture);

        if (showSnackbar)
        {
            CreateSnackbarMessage("Successfully listed SSH Logins!", Severity.Success);
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