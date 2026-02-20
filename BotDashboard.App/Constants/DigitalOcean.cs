namespace BotDashboard.App.Constants;

public class DigitalOcean
{
    public readonly string Host;
    public readonly string Username;
    public readonly string Password;

    public DigitalOcean(string host, string username, string password)
    {
        Host = host;
        Username = username;
        Password = password;
    }
}