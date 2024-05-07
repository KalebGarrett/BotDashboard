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

    public string RunningContainers()
    {
        return "docker ps";
    }

    public string Stats()
    {
        return "docker ls";
    }

    public string Pull(string imageName)
    {
        return $"docker pull {imageName}";
    }
}
