using System.Globalization;
using BotDashboard.App.Services;
using BotDashboard.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace BotDashboard.App.Components.Pages;

public partial class Home
{
    [Inject] private DockerService DockerService { get; set; }
    [Inject] private UbuntuService UbuntuService { get; set; }
   
    private List<Container> Containers { get; set; } = new();
    private string ContainerFetchTime { get; set; }
    
    public double[] MemoryUsageData { get; set; }
    public string[] MemoryUsageLabels { get; set; } = ["Used Memory", "Total Memory"];
    private static double MemoryUsagePercentage { get; set; }
    
    private double[] CpuUsageData { get; set; }
    private string[] CpuUsageLabels { get; set; } = ["Used CPU", "Total CPU"];
    private static double CpuUsagePercentage { get; set; }
    
    private string StatFetchTime { get; set; }

    protected override async Task OnInitializedAsync()
    {
      if (Switch.OnInitialized == false)
      {
          MemoryUsagePercentage = 0;
          await Task.CompletedTask;
          return;
      }
      
      ListContainers(showSnackbar: false);
      ListStats(showSnackbar: false);
    }
    
    private void RunImage(string imageName)
    {
        DockerService.RunImage(imageName);
        ListContainers(showSnackbar: false);
        CreateSnackbarMessage("Successfully ran image!", Severity.Success);
    }

    private void StopContainer(string containerId)
    {
        DockerService.StopContainer(containerId);
        ListContainers(showSnackbar: false);
        CreateSnackbarMessage("Successfully stopped container!", Severity.Success);
    }

    private void StopAllContainers()
    {
        DockerService.StopAllContainers();
        ListContainers(showSnackbar: false);
        CreateSnackbarMessage("Successfully stopped all containers!", Severity.Success);
    }

    private void RestartContainer(string containerId)
    {
        DockerService.RestartContainer(containerId);
        ListContainers(showSnackbar: false);
        CreateSnackbarMessage("Successfully restarted container!", Severity.Success);
    }

    private void RestartAllContainers()
    {
        DockerService.RestartAllContainers();
        ListContainers(showSnackbar: false);
        CreateSnackbarMessage("Successfully restarted all containers!", Severity.Success);
    }

    private void ListContainers(bool showSnackbar = true)
    {
        Containers = DockerService.ListContainers();
        ContainerFetchTime = DateTime.UtcNow.ToLocalTime().ToString(CultureInfo.CurrentCulture);

        if (showSnackbar)
        {
            CreateSnackbarMessage("Successfully listed containers!", Severity.Success);
        }
    }
    
    private void ListStats(bool showSnackbar = true)
    {
        MemoryUsagePercentage = UbuntuService.MemoryUsagePercentage();
        MemoryUsageData = [MemoryUsagePercentage, 100 - MemoryUsagePercentage];
        
        CpuUsagePercentage = UbuntuService.CpuUsagePercentage();
        CpuUsageData = [CpuUsagePercentage, 100 - CpuUsagePercentage];
        
        StatFetchTime = DateTime.UtcNow.ToLocalTime().ToString(CultureInfo.CurrentCulture);
        
        if (showSnackbar)
        {
            CreateSnackbarMessage("Successfully listed stats!", Severity.Success);
        }
    }
    
    private void CreateSnackbarMessage(string message, Severity severity)
    {
        Snackbar.Configuration.PositionClass = Defaults.Classes.Position.BottomRight;
        Snackbar.Add(message, severity);
    }
}