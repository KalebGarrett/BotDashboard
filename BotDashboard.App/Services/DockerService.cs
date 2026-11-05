using BotDashboard.App.Commands;
using BotDashboard.App.Credentials;
using BotDashboard.Models;
using Renci.SshNet;

namespace BotDashboard.App.Services;

public class DockerService
{
    private readonly NasCredentials _nas;

    public DockerService(NasCredentials nas)
    {
        _nas = nas;
    }

    private DockerCommand DockerCommand { get; } = new();
    
    public void StartContainer(string containerName)
    {
        var command = DockerCommand.Start(containerName);
        RunCommand(command);
    }

    public void StopContainer(string containerId)
    {
        var command = DockerCommand.Stop(containerId);
        RunCommand(command);
    }

    public void StopAllContainers()
    {
        var command = DockerCommand.StopAll();
        RunCommand(command);
    }

    public void RestartContainer(string containerId)
    {
        var command = DockerCommand.Restart(containerId);
        RunCommand(command);
    }
    
    public void RestartAllContainers()
    {
        var command = DockerCommand.RestartAll();
        RunCommand(command);
    }
    
    public List<Container> ListContainers()
    {
        if (!_nas.IsConfigured)
        {
            throw new InvalidOperationException(
                "Cortex SSH settings are missing. Set NAS_HOST, NAS_USERNAME and NAS_PASSWORD "
                + "in .env, then restart the app. See deploy/README.md.");
        }

        using var client = new SshClient(_nas.Host, _nas.Username, _nas.Password);
        client.Connect();

        var command = client.CreateCommand(DockerCommand.ListContainers());
        var response = command.Execute();

        client.Disconnect();
        
        // Tab separated by DockerCommand, and with no header row to skip.
        return response.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Split('\t'))
            .Where(fields => fields.Length >= 6)
            .Select(fields => new Container
            {
                ContainerId = fields[0].Trim(),
                Image = fields[1].Trim(),
                Command = fields[2].Trim(),
                Created = fields[3].Trim(),
                Status = fields[4].Trim(),
                Names = fields[5].Trim()
            })
            .OrderBy(container => container.Image)
            .ToList();
    }
    
    public List<DockerImage> ListImages()
    {
        if (!_nas.IsConfigured)
        {
            throw new InvalidOperationException(
                "Cortex SSH settings are missing. Set NAS_HOST, NAS_USERNAME and NAS_PASSWORD "
                + "in .env, then restart the app. See deploy/README.md.");
        }

        using var client = new SshClient(_nas.Host, _nas.Username, _nas.Password);
        client.Connect();

        var command = client.CreateCommand(DockerCommand.ListImages());
        var response = command.Execute();

        client.Disconnect();
        
        // Tab separated by DockerCommand, and with no header row to skip.
        return response.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Split('\t'))
            .Where(fields => fields.Length >= 5)
            .Select(fields => new DockerImage
            {
                Repository = fields[0].Trim(),
                Tag = fields[1].Trim(),
                ImageId = fields[2].Trim(),
                Created = fields[3].Trim(),
                Size = fields[4].Trim()
            })
            .OrderBy(image => image.Repository)
            .ToList();
    }
    
    public string PullImage(string imageName)
    {
        var command = DockerCommand.Pull(imageName);
        var response = RunCommandWithResponse(command);

        return response;
    }
    
    public string RemoveImage(string imageId)
    {
        var command = DockerCommand.Remove(imageId);
        var response = RunCommandWithResponse(command);

        return response;
    }

    public string LogContainerCommand(string containerId)
    {
        var command = DockerCommand.Log(containerId);
        var response = RunCommandWithResponse(command);

        return response;
    }
    
    private void RunCommand(string command)
    {
        if (!_nas.IsConfigured)
        {
            throw new InvalidOperationException(
                "Cortex SSH settings are missing. Set NAS_HOST, NAS_USERNAME and NAS_PASSWORD "
                + "in .env, then restart the app. See deploy/README.md.");
        }

        using var client = new SshClient(_nas.Host, _nas.Username, _nas.Password);
        client.Connect();
        client.RunCommand(command);
        client.Disconnect();
    }

    private string RunCommandWithResponse(string command)
    {
        if (!_nas.IsConfigured)
        {
            throw new InvalidOperationException(
                "Cortex SSH settings are missing. Set NAS_HOST, NAS_USERNAME and NAS_PASSWORD "
                + "in .env, then restart the app. See deploy/README.md.");
        }

        using var client = new SshClient(_nas.Host, _nas.Username, _nas.Password);
        client.Connect();
         
        var response = client.RunCommand(command).Execute();
        
        client.Disconnect();

        return response;
    }
}