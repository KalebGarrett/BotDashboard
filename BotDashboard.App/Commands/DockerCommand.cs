namespace BotDashboard.App.Commands;

public class DockerCommand
{
    public string Run(string imageName)
    {
        return $"docker run {imageName}";
    }

    public string Stop(string containerId)
    {
        return $"docker stop {containerId}";
    }

    public string StopAll()
    {
        return "docker stop $(docker ps -q)";
    }

    public string ListImages()
    {
        return "docker images";
    }
    
    public string ListContainers()
    {
        return "docker ps";
    }
    
    public string Stats()
    {
        return "docker stats";
    }

    public string Pull(string imageName)
    {
        return $"docker pull {imageName}";
    }
}
