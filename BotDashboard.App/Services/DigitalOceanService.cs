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

    /// <summary>
    /// Parses the configured droplet id.
    /// </summary>
    /// <remarks>
    /// <c>Convert.ToInt64</c> on a blank or malformed id throws <see cref="FormatException"/>
    /// with the message "The input string '' was not in a correct format", which says nothing
    /// about which setting is wrong. Worse, thrown from a component's OnInitializedAsync it
    /// tears down the Blazor circuit, so the page dies rather than showing the problem.
    /// </remarks>
    private long DropletId()
    {
        var id = _digitalOceanDropletCredentials.Id;

        if (string.IsNullOrWhiteSpace(id))
        {
            throw new InvalidOperationException(
                "DROPLET_ID is not set. Add it to .env and restart the app. See deploy/README.md.");
        }

        if (!long.TryParse(id, out var dropletId))
        {
            throw new InvalidOperationException(
                $"DROPLET_ID is '{id}', which is not a number. It is the numeric id from the "
                + "droplet's DigitalOcean URL.");
        }

        return dropletId;
    }

    public async Task<Droplet> GetDropletInfo()
    {
        var response = await _digitalOceanClient.Droplets.Get(DropletId());
        return response;
    }

    public async Task PowerOnDroplet()
    {
        await _digitalOceanClient.DropletActions.PowerOn(DropletId());
    }

    public async Task PowerOffDroplet()
    {
        await _digitalOceanClient.DropletActions.PowerOff(DropletId());
    }

    public async Task RebootDroplet()
    {
        await _digitalOceanClient.DropletActions.Reboot(DropletId());
    }
}