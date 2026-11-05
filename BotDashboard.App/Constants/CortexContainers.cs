namespace BotDashboard.App.Constants;

/// <summary>What a container is for, which is how the command cards group their buttons.</summary>
public enum ContainerGroup
{
    /// <summary>A plain Discord bot: an outbound websocket and nothing else.</summary>
    Bot,

    /// <summary>Jam Junction and the pieces only it needs.</summary>
    Audio,

    /// <summary>Things the stacks run on rather than things that do the work.</summary>
    Infrastructure,

    /// <summary>UGOS App Center containers that came with the NAS.</summary>
    App
}

/// <summary>One container that exists on Cortex and can be driven from the dashboard.</summary>
/// <param name="Name">The container name, which is what every docker action is addressed to.</param>
/// <param name="Display">What the dashboard calls it.</param>
/// <param name="Image">The image to pull, or null when the dashboard does not manage updates for it.</param>
/// <param name="Group">Which section of the command card it belongs in.</param>
public sealed record CortexContainer(string Name, string Display, string Image, ContainerGroup Group);

/// <summary>
/// The containers on Cortex, as the command cards present them.
/// </summary>
/// <remarks>
/// Every one of these is created by a compose file, so the dashboard addresses them by
/// <c>container_name</c> and starts them with <c>docker start</c>. It deliberately does not
/// <c>docker run</c> an image: that creates a second, anonymous container outside the stack,
/// with none of its environment or networking. For a Discord bot that is worse than useless --
/// Discord allows one gateway connection per token, so a second copy makes the bot answer every
/// command twice and the two instances fight over the same interactions.
///
/// <c>botharbor-app</c> is absent on purpose. It is the dashboard; offering a button that stops
/// the thing rendering the button is a trap, not a feature.
/// </remarks>
public static class CortexContainers
{
    public static readonly CortexContainer[] All =
    [
        new("jokebot", "Joke Bot", DockerRepository.JokeBot, ContainerGroup.Bot),
        new("triviabot", "Trivia Bot", DockerRepository.TriviaBot, ContainerGroup.Bot),
        new("factbot", "Fact Bot", DockerRepository.FactBot, ContainerGroup.Bot),
        new("prembot", "Prem Bot", DockerRepository.PremBot, ContainerGroup.Bot),

        new("jamjunction", "Jam Junction", DockerRepository.JamJunction, ContainerGroup.Audio),
        new("yt-cipher", "YouTube Cipher", DockerRepository.YtCipher, ContainerGroup.Audio),
        // Jam Junction has no network stack of its own; it runs inside this one's namespace,
        // so starting Jam Junction without this first gets nowhere.
        new("bots-tailscale", "Tailscale (Bots)", null, ContainerGroup.Audio),

        new("botharbor-postgres", "PostgreSQL", null, ContainerGroup.Infrastructure),
        new("botharbor-cloudflared", "Cloudflare Tunnel", null, ContainerGroup.Infrastructure),

        new("jellyfin-app-1", "Jellyfin", null, ContainerGroup.App),
        new("firefox-app-1", "Firefox", null, ContainerGroup.App)
    ];

    /// <summary>The containers in one group, in catalogue order.</summary>
    public static IEnumerable<CortexContainer> In(ContainerGroup group) =>
        All.Where(container => container.Group == group);

    /// <summary>Everything the dashboard knows how to pull a fresh image for.</summary>
    public static IEnumerable<CortexContainer> Pullable =>
        All.Where(container => !string.IsNullOrWhiteSpace(container.Image));

    /// <summary>
    /// Containers a fleet-wide stop or restart must leave alone.
    /// </summary>
    /// <remarks>
    /// Stopping these takes the dashboard off the air mid-click: the app serves the page, the
    /// tunnel carries it, and Postgres holds the session. The operator would be left with a
    /// Cloudflare 502 and no way back in short of an SSH session.
    /// </remarks>
    public static readonly string[] Protected =
    [
        "botharbor-app",
        "botharbor-postgres",
        "botharbor-cloudflared"
    ];
}
