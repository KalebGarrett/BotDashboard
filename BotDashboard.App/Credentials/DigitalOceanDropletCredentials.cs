namespace BotDashboard.App.Credentials;

public class DigitalOceanDropletCredentials
{
    public readonly string Id;
    public readonly string Host;
    public readonly string Username;
    public readonly string Password;

    public DigitalOceanDropletCredentials(string id, string host, string username, string password)
    {
        Id = id;
        Host = host;
        Username = username;
        Password = password;
    }
}