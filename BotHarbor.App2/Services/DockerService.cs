using BotDashboard.App.Commands;
using BotDashboard.App.Secrets;
using BotDashboard.Models;
using Renci.SshNet;

namespace BotDashboard.App.Services;

public class DockerService
{
    private readonly DockerCommand _dockercommand;

    public DockerService(DockerCommand dockercommand)
    {
        _dockercommand = dockercommand;
    }

    public void RunImage(string imageName)
    {
        var command = _dockercommand.Run(imageName);
        RunCommand(command);
    }

    public void RunYtCipher()
    {
        var command = _dockercommand.RunYtCipher();
        RunCommand(command);
    }

    public void StopContainer(string containerId)
    {
        var command = _dockercommand.Stop(containerId);
        RunCommand(command);
    }

    public void StopAllContainers()
    {
        var command = _dockercommand.StopAll();
        RunCommand(command);
    }

    public void RestartContainer(string containerId)
    {
        var command = _dockercommand.Restart(containerId);
        RunCommand(command);
    }
    
    public void RestartAllContainers()
    {
        var command = _dockercommand.RestartAll();
        RunCommand(command);
    }
    
    public List<Container> ListContainers()
    {
        using var client = new SshClient(DigitalOcean.Host, DigitalOcean.Username, DigitalOcean.Password);
        client.Connect();

        var command = client.CreateCommand(_dockercommand.ListContainers());
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
        using var client = new SshClient(DigitalOcean.Host, DigitalOcean.Username, DigitalOcean.Password);
        client.Connect();

        var command = client.CreateCommand(_dockercommand.ListImages());
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
        var command = _dockercommand.Pull(imageName);
        var response = RunCommandWithResponse(command);

        return response;
    }
    
    public string RemoveImage(string imageId)
    {
        var command = _dockercommand.Remove(imageId);
        var response = RunCommandWithResponse(command);

        return response;
    }

    public string LogContainerCommand(string containerId)
    {
        var command = _dockercommand.Log(containerId);
        var response = RunCommandWithResponse(command);

        return response;
    }
    
    private void RunCommand(string command)
    {
        using var client = new SshClient(DigitalOcean.Host, DigitalOcean.Username, DigitalOcean.Password);
        client.Connect();
        client.RunCommand(command);
        client.Disconnect();
    }

    private string RunCommandWithResponse(string command)
    {
        using var client = new SshClient(DigitalOcean.Host, DigitalOcean.Username, DigitalOcean.Password);
        client.Connect();
         
        var response = client.RunCommand(command).Execute();
        
        client.Disconnect();

        return response;
    }
}