namespace BotDashboard.App.Commands;

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

    public string RunningContainers()
    {
        return "docker ps";
    }

    public string Stats()
    {
        return "docker ls";
    }

    public string ShowImages()
    {
        return "docker images";
    }

    public string Pull(string imageName)
    {
        return $"docker pull {imageName}";
    }
}
