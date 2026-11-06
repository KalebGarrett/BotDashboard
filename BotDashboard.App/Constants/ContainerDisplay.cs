using BotDashboard.Models;

namespace BotDashboard.App.Constants;

/// <summary>
/// Turns what Docker reports into something readable on the dashboard.
/// </summary>
/// <remarks>
/// <c>docker ps</c> gives a container name and an image reference, and neither is meant for
/// people: "kalebg08/jamjunction:latest" and "botharbor-postgres" say what is running, not what
/// it is. The container name is the better source of the two, because on Cortex every container
/// is named by its compose service, so it survives an image retag.
/// </remarks>
public static class ContainerDisplay
{
    /// <summary>Docker's own mark, used for anything without a more specific icon.</summary>
    public const string DockerIcon = "imgs/Docker Logo.png";

    // Matched against the container name first, then the image. Order matters: the first
    // substring that matches wins, so put the more specific keys above the general ones.
    private static readonly (string Key, string Name)[] KnownNames =
    {
        ("jamjunction", "Jam Junction"),
        ("jokebot", "Joke Bot"),
        ("triviabot", "Trivia Bot"),
        ("factbot", "Fact Bot"),
        ("prembot", "Prem Bot"),
        ("botharbor-postgres", "PostgreSQL"),
        ("botharbor-cloudflared", "Cloudflare Tunnel"),
        ("botharbor-tailscale", "Tailscale"),
        ("bots-tailscale", "Tailscale (Bots)"),
        ("botharbor-app", "Bot Harbor"),
        ("cloudflared", "Cloudflare Tunnel"),
        ("tailscale", "Tailscale"),
        ("postgres", "PostgreSQL"),
        ("jellyfin", "Jellyfin"),
        ("firefox", "Firefox"),
        ("ollama", "Ollama")
    };

    public static string Name(Container container)
    {
        if (container is null)
        {
            return "Unknown";
        }

        var fromName = Lookup(container.Names);
        if (fromName is not null)
        {
            return fromName;
        }

        var fromImage = Lookup(container.Image);
        if (fromImage is not null)
        {
            return fromImage;
        }

        // Nothing recognised. Docker's generated names look like "vigorous_stonebraker", which
        // is noise, so fall back to the image's last path segment with its tag stripped.
        return Humanise(FallbackSource(container));
    }

    private static string FallbackSource(Container container)
    {
        var image = container.Image ?? string.Empty;
        var withoutTag = image.Split(':')[0];
        var lastSegment = withoutTag.Split('/')[^1];

        return string.IsNullOrWhiteSpace(lastSegment) ? container.Names ?? "Unknown" : lastSegment;
    }

    private static string Lookup(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        foreach (var (key, name) in KnownNames)
        {
            if (value.Contains(key, StringComparison.OrdinalIgnoreCase))
            {
                return name;
            }
        }

        return null;
    }

    private static string Humanise(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Unknown";
        }

        var words = value.Replace('_', ' ').Replace('-', ' ').Split(' ',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return string.Join(" ", words.Select(word =>
            char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant()));
    }

    /// <summary>The icon for a container: Docker's mark for all of them.</summary>
    public static string IconUrl(string image) => DockerIcon;

    /// <summary>
    /// Maps a <c>docker ps</c> status string onto a <c>.bh-status--*</c> CSS modifier.
    /// </summary>
    /// <remarks>
    /// Docker reports things like "Up 2 hours", "Up 5 minutes (unhealthy)", "Restarting (1) 3
    /// seconds ago", "Exited (0) 5 minutes ago" and "Created". Lives here rather than on a page
    /// because the dashboard and the Commands page draw the same card and must agree on what
    /// counts as healthy.
    /// </remarks>
    public static string StatusModifier(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return "bh-status--idle";
        }

        if (status.StartsWith("Up", StringComparison.OrdinalIgnoreCase))
        {
            // "Up 5 minutes (unhealthy)" is running, but not happily.
            return status.Contains("unhealthy", StringComparison.OrdinalIgnoreCase)
                ? "bh-status--idle"
                : "bh-status--up";
        }

        if (status.Contains("Exit", StringComparison.OrdinalIgnoreCase)
            || status.Contains("Dead", StringComparison.OrdinalIgnoreCase))
        {
            return "bh-status--down";
        }

        return "bh-status--idle";
    }
}
