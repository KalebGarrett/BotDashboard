using System.Globalization;
using BotDashboard.App.Services;
using Microsoft.AspNetCore.Components;

namespace BotDashboard.App.Components.Pages;

public partial class Stats
{
    [Inject] private UbuntuService UbuntuService { get; set; }
    
    private static double MemoryUsagePercentage { get; set; }
    private double[] MemoryUsageData { get; set; }
    private string[] MemoryUsageLabels { get; set; } = {"Used Memory", "Total Memory"};
    private string MemoryFetchTime { get; set; }
    
    private static double CpuUsagePercentage { get; set; }
    private double[] CpuUsageData { get; set; }
    private string[] CpuUsageLabels { get; set; } = {"Used CPU", "Total CPU"};
    private string CpuFetchTime { get; set; }

    protected override async Task OnInitializedAsync()
    {
        if (Switch.OnInitialized == false)
        {
            MemoryUsagePercentage = 0;
            CpuUsagePercentage = 0;
            await Task.CompletedTask;
            return;
        }
        
        ListMemoryUsagePercentage();
        ListCpuUsagePercentage();
    }
    
    private void ListMemoryUsagePercentage()
    {
        MemoryUsagePercentage = UbuntuService.MemoryUsagePercentage();
        MemoryUsageData = [MemoryUsagePercentage, 100 - MemoryUsagePercentage];
        MemoryFetchTime = DateTime.UtcNow.ToLocalTime().ToString(CultureInfo.CurrentCulture);
    }

    private void ListCpuUsagePercentage()
    {
        CpuUsagePercentage = UbuntuService.CpuUsagePercentage();
        CpuUsageData = [CpuUsagePercentage, 100 - CpuUsagePercentage];
        CpuFetchTime = DateTime.UtcNow.ToLocalTime().ToString(CultureInfo.CurrentCulture);
    }
}