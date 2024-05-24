﻿using BotDashboard.App.Services;
using BotDashboard.Models;
using Microsoft.AspNetCore.Components;

namespace BotDashboard.App.Components.Pages;

public partial class Commands
{
    [Inject] public DockerService DockerService { get; set; }
    private List<DockerImage> DockerImages { get; set; } = new();
    private string[] Headings { get; set; } = {"Repository", "Tag", "Image Id", "Created", "Size"};

    protected override async Task OnInitializedAsync()
    {
        ListImages();
    }

    private void ListImages()
    {
        DockerImages = DockerService.ListImages();
    }
}