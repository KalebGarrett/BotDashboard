namespace BotDashboard.App.Commands;

public class UbuntuCommand
{
    public string ListMemoryUsage()
    {
        return "wmic OS get FreePhysicalMemory /value | findstr /R \"[0-9]\"";
    }

    public string ListCpuUsage()
    {
        return "wmic cpu get loadpercentage | findstr /R \"[0-9]\"";
    }
}
