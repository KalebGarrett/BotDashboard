using BotDashboard.App.Constants;

namespace BotDashboard.App.Commands;

public class DockerCommand
{
    /// <summary>Starts an existing container by name.</summary>
    /// <remarks>
    /// Not <c>docker run</c>. Every container on Cortex is created by a compose file, so running
    /// the image again builds a second, anonymous container with none of the stack's environment
    /// or networking -- and for a Discord bot that means two gateway connections on one token,
    /// so every command gets answered twice. See <see cref="Constants.CortexContainers"/>.
    /// </remarks>
    public string Start(string containerName)
    {
        return $"docker start {containerName}";
    }

    public string Stop(string containerId)
    {
        return $"docker stop {containerId}";
    }

    /// <summary>Stops every running container except the ones serving this page.</summary>
    /// <remarks>
    /// This used to be <c>docker stop $(docker ps -q)</c>, which included the dashboard, its
    /// database and its tunnel -- one click and the operator was looking at a Cloudflare 502
    /// with no way back except SSH. The exclusions come from
    /// <see cref="Constants.CortexContainers.Protected"/>; anchoring the pattern matters so a
    /// future container whose name merely contains "botharbor-app" is not spared by accident.
    /// </remarks>
    public string StopAll()
    {
        return $"docker ps --format '{{{{.Names}}}}' | grep -vxE '{ProtectedPattern()}' "
               + "| xargs -r docker stop";
    }

    public string Restart(string containerId)
    {
        return $"docker restart {containerId}";
    }

    /// <summary>Restarts every running container except the ones serving this page.</summary>
    public string RestartAll()
    {
        return $"docker ps --format '{{{{.Names}}}}' | grep -vxE '{ProtectedPattern()}' "
               + "| xargs -r docker restart";
    }

    private static string ProtectedPattern()
    {
        return string.Join('|', Constants.CortexContainers.Protected);
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