using BotDashboard.App.Secrets;
using Renci.SshNet.Common;

namespace BotDashboard.App.Events;

public static class SshEvents
{
    public static void ClientOnHostKeyReceived(object sender, HostKeyEventArgs e)
    {
        var expectedFingerPrint = DigitalOcean.FingerPrint;
        e.CanTrust = expectedFingerPrint.Equals(e.FingerPrintSHA256);
    }
}