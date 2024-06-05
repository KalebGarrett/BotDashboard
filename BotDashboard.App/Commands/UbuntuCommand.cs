namespace BotDashboard.App.Commands;

public class UbuntuCommand
{
    public string ListMemoryUsage()
    {
        return "free | awk 'FNR == 2 {print \"Memory Usage: \" $3/$2*100 \"%\"}'";
    }
}