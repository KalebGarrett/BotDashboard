using BotDashboard.App.Commands;
using BotDashboard.App.Constants;
using BotDashboard.Models;
using Renci.SshNet;

namespace BotDashboard.App.Services;

public class DockerService
{
    private readonly DigitalOcean _digitalOcean;

    public DockerService(DigitalOcean digitalOcean)
    {
        _digitalOcean = digitalOcean;
    }

    private DockerCommand DockerCommand { get; } = new();
    
    public void RunImage(string imageName)
    {
        var command = DockerCommand.Run(imageName);
        RunCommand(command);
    }

    public void RunYtCipher()
    {
        var command = DockerCommand.RunYtCipher();
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
        using var client = new SshClient(_digitalOcean.Host, _digitalOcean.Username, _digitalOcean.Password);
        client.Connect();

        var command = client.CreateCommand(DockerCommand.ListContainers());
        var response = command.Execute();

        client.Disconnect();
        
        return response.Split("\n")
            .Skip(1)
            .SkipLast(1)
            .Select(str => str.Split("   ")
                .Where(s => s != "").ToArray())
            .Select(row => new Container
            {
                ContainerId = row[0].Trim(),
                Image = row[1].Trim(),
                Command = row[2].Trim(),
                Created = row[3].Trim(),
                Status = row[4].Trim(),
                Names = row[5].Trim()
            })
            .OrderBy(container => container.Image)
            .ToList();
    }
    
    public List<DockerImage> ListImages()
    {
        using var client = new SshClient(_digitalOcean.Host, _digitalOcean.Username, _digitalOcean.Password);
        client.Connect();

        var command = client.CreateCommand(DockerCommand.ListImages());
        var response = command.Execute();

        client.Disconnect();
        
        return response.Split("\n")
            .Skip(1)
            .SkipLast(1)
            .Select(str => str.Split("   ")
                .Where(s => s != "").ToArray())
            .Select(row => new DockerImage
            {
                Repository = row[0].Trim(),
                Tag = row[1].Trim(),
                ImageId = row[2].Trim(),
                Created = row[3].Trim(),
                Size = row[4].Trim(),
            })
            .OrderBy(container => container.Repository)
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
        using var client = new SshClient(_digitalOcean.Host, _digitalOcean.Username, _digitalOcean.Password);
        client.Connect();
        client.RunCommand(command);
        client.Disconnect();
    }

    private string RunCommandWithResponse(string command)
    {
        using var client = new SshClient(_digitalOcean.Host, _digitalOcean.Username, _digitalOcean.Password);
        client.Connect();
         
        var response = client.RunCommand(command).Execute();
        
        client.Disconnect();

        return response;
    }
}