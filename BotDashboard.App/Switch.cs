namespace BotDashboard.App;

/// <summary>
/// Whether a page fetches live data as soon as it opens.
/// </summary>
/// <remarks>
/// Off, every page renders instantly with placeholders and waits for an explicit refresh; on,
/// opening a page costs several SSH round trips to Cortex. Which is wanted depends on the day,
/// so it is an operator preference rather than a constant.
///
/// Static because it is a single-operator setting, and seeded from
/// <see cref="Services.PreferencesService"/> at startup so it survives a restart. It used to
/// reset to false on every deploy, which made it look like the toggle did not work.
/// </remarks>
public static class Switch
{
    public static bool OnInitialized { get; set; }
}
