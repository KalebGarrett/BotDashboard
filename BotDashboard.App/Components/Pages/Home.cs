using System.Globalization;
using BotDashboard.App.Services;
using BotDashboard.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace BotDashboard.App.Components.Pages;

public partial class Home
{
    [Inject]
    private DockerService DockerService { get; set; }
    private List<Container> Containers { get; set; } = new();
    private string Time { get; set; }
    private int Index { get; set; } = -1;
    public ChartOptions Options { get; set; } = new ChartOptions();
    public List<ChartSeries> Series { get; set;  }= new()
    {
        new ChartSeries() { Name = "Fossil", Data = new double[] { 90, 79, 72, 69, 62, 62, 55, 65, 70 } },
        new ChartSeries() { Name = "Renewable", Data = new double[] { 10, 41, 35, 51, 49, 62, 69, 91, 148 } },
    };
    public string[] XAxisLabels { get; set; } = {"Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep" };
    
    protected override async Task OnInitializedAsync()
    {
        ListContainers();
    }
    
    private void RunImage(string imageName)
    {
        DockerService.RunImage(imageName);
        //Add locking mechanism if already running
        Task.Delay(TimeSpan.FromSeconds(3));
        ListContainers();
    }
    
    private void StopImage(string containerId)
    {
        DockerService.StopImage(containerId);
        Task.Delay(TimeSpan.FromSeconds(3));
        ListContainers();
    }

    private void StopAllImages()
    {
        DockerService.StopAllImages();
        Task.Delay(TimeSpan.FromSeconds(3));
        ListContainers();
    }
    
    private void RestartAllImages()
    {
        DockerService.RestartAllImages();
        Task.Delay(TimeSpan.FromSeconds(3));
        ListContainers();
    }
    
    private void ListContainers()
    {
        Containers = DockerService.ListContainers();
        Time = DateTime.UtcNow.ToLocalTime().ToString(CultureInfo.CurrentCulture);
    }
}