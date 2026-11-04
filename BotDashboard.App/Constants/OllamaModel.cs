namespace BotDashboard.App.Constants;

/// <summary>
/// Ollama models used by Jam Junction's song recommender.
/// </summary>
public class OllamaModel
{
    /// <summary>
    /// The model the droplet actually runs. Selected at run time through the
    /// <c>OLLAMA_MODEL</c> environment variable on the Jam Junction container, because the
    /// 4 GB droplet cannot hold the larger default.
    /// </summary>
    public const string Llama32_1B = "llama3.2:1b";

    /// <summary>The image's baked-in default, used on machines with memory to spare.</summary>
    public const string Llama32 = "llama3.2";
}
