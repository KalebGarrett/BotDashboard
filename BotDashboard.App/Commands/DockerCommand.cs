using BotDashboard.App.Constants;

namespace BotDashboard.App.Commands;

public class DockerCommand
{
    public string Run(string imageName)
    {
        // Jam Junction runs the AI song recommender against an Ollama server on the
        // droplet's loopback (127.0.0.1:11434). Host networking lets the container
        // reach that loopback, and OLLAMA_MODEL selects the 1B model that fits the
        // 4 GB droplet (the image's baked-in default is the larger 3B model).
        if (imageName == DockerRepository.JamJunction)
            return $"docker run -d --network host -e OLLAMA_MODEL=llama3.2:1b {imageName}";

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

    public string Restart(string containerId)
    {
        return $"docker restart {containerId}";
    }
    
    public string RestartAll()
    {
        return "docker restart $(docker ps -q)";
    }

    /// <summary>Every image, one per line, tab separated.</summary>
    /// <remarks>
    /// Formatted explicitly rather than parsed out of the default table. Docker 29 -- which is
    /// what Cortex runs -- replaced that table's columns outright: it now prints
    /// IMAGE / ID / DISK USAGE / CONTENT SIZE / EXTRA instead of
    /// REPOSITORY / TAG / IMAGE ID / CREATED / SIZE. Reading by position therefore put the size
    /// in the image id column and the content size, "0B", under Created. A Go template pins the
    /// fields whatever Docker the host happens to run, and a tab beats the old
    /// split-on-three-spaces guess because no field can contain one.
    /// </remarks>
    public string ListImages()
    {
        return "docker images --format "
               + "'{{.Repository}}\t{{.Tag}}\t{{.ID}}\t{{.CreatedSince}}\t{{.Size}}'";
    }

    /// <summary>Every running container, one per line, tab separated.</summary>
    /// <remarks>See <see cref="ListImages"/> for why these are formatted rather than parsed.</remarks>
    public string ListContainers()
    {
        return "docker ps --format "
               + "'{{.ID}}\t{{.Image}}\t{{.Command}}\t{{.RunningFor}}\t{{.Status}}\t{{.Names}}'";
    }
    
    public string Pull(string imageName)
    {
        return $"docker pull {imageName}";
    }

    public string Remove(string imageId)
    {
        return $"docker rmi -f {imageId}";
    }

    public string Log(string containerId)
    {
        return $"docker logs --timestamps {containerId}";
    }
    
    public string Stats()
    {
        return "docker stats";
    }
}