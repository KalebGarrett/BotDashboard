namespace BotDashboard.App;

public class Switch
{
    public static bool OnInitialized { get; set; }

    public static void ToggleOnInitialized()
    {
        OnInitialized = !OnInitialized;
    }
}