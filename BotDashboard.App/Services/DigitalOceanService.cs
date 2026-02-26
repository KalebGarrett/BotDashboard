using BotDashboard.App.Credentials;
using DigitalOcean.API;
using DigitalOcean.API.Models.Responses;

namespace BotDashboard.App.Services;

public class DigitalOceanService
{
    private readonly DigitalOceanDropletCredentials _digitalOceanDropletCredentials;
    private readonly DigitalOceanClient _digitalOceanClient;

    public DigitalOceanService(DigitalOceanDropletCredentials digitalOceanDropletCredentials, DigitalOceanClient digitalOceanClient)
    {
        _digitalOceanDropletCredentials = digitalOceanDropletCredentials;
        _digitalOceanClient = digitalOceanClient;
    }

    public async Task<Droplet> GetDropletInfo()
    {
        var response = await _digitalOceanClient.Droplets.Get(Convert.ToInt64(_digitalOceanDropletCredentials.Id));
        return response;
    }

    public async Task PowerOnDroplet()
    {
        await _digitalOceanClient.DropletActions.PowerOn(Convert.ToInt64(_digitalOceanDropletCredentials.Id));
    }

    public async Task PowerOffDroplet()
    {
        await _digitalOceanClient.DropletActions.PowerOff(Convert.ToInt64(_digitalOceanDropletCredentials.Id));
    }

    public async Task RebootDroplet()
    {
        await _digitalOceanClient.DropletActions.Reboot(Convert.ToInt64(_digitalOceanDropletCredentials.Id));
    }
}