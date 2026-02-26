namespace BotDashboard.App.Credentials;

public class DigitalOceanCredentials
{
    public readonly string ApiToken;

    public DigitalOceanCredentials(string apiToken)
    {
        ApiToken = apiToken;
    }
}