using BotDashboard.App.Services;
using BotDashboard.Models;
using Microsoft.AspNetCore.Components;

namespace BotDashboard.App.Components.Pages;

public partial class Commands
{
    [Inject]
    public DockerService DockerService { get; set; }
    private List<Images> Images { get; set; } = new List<Images>();

    protected override async Task OnInitializedAsync()
    {
        ListImages();
    }
    
    private void ListImages()
    {
        Images = DockerService.ListImages();
    }
}