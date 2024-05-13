﻿namespace BotDashboard.App.Commands;

public class DockerCommand
{
    public string Run(string imageName)
    {
        return $"docker run {imageName}";
    }

    public string Stop(string containerName)
    {
        return $"docker stop {containerName}";
    }

    public string StopAll()
    {
        return "docker stop $(docker ps -q)";
    }
    
    public string Pull(string imageName)
    {
        return $"docker pull {imageName}";
    }
}
