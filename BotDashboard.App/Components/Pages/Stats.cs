using System.Globalization;
using BotDashboard.App.Services;
using BotDashboard.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace BotDashboard.App.Components.Pages;

public partial class Stats
{
    [Inject] private DockerService DockerService { get; set; }
    [Inject] private UbuntuService UbuntuService { get; set; }
    private static double MemoryUsagePercentage { get; set; }
    private double[] DropletData { get; set; } = {MemoryUsagePercentage, 100 - MemoryUsagePercentage};
    private string[] DropletLabels { get; set; } = {"Used Memory", "Total Memory"};
    private string DropletMemoryFetchTime { get; set; }
    private int Index = -1;
    private ChartOptions Options { get; set; } = new ChartOptions();
    private List<ChartSeries> Series { get; set; } = new List<ChartSeries>()
    {
        new ChartSeries() { Name = "Fossil", Data = new double[] { 90, 79, 72, 69, 62, 62, 55, 65, 70 } },
        new ChartSeries() { Name = "Renewable", Data = new double[] { 10, 41, 35, 51, 49, 62, 69, 91, 148 } },
    };
    private string[] XAxisLabels { get; set; } = {"Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep" };
    
    private void ListDropletMemoryUsagePercentage()
    {
        MemoryUsagePercentage = Convert.ToDouble(UbuntuService.MemoryUsagePercentage());
        DropletMemoryFetchTime = DateTime.UtcNow.ToLocalTime().ToString(CultureInfo.CurrentCulture);
    }
}