namespace BotDashboard.App.Commands;

public class UbuntuCommand
{
    public string ListMemoryStats()
    {
        return "df -h /";
    }
}