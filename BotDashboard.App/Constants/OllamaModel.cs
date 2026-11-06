namespace BotDashboard.App.Constants;

/// <summary>
/// Ollama models used by Jam Junction's song recommender.
/// </summary>
public class OllamaModel
{
    /// <summary>
    /// The model Jam Junction is pointed at today, through the <c>OLLAMA_MODEL</c> environment
    /// variable on its container. Chosen when Ollama lived on a 4 GB droplet; the server PC has
    /// room for the larger default if that is ever wanted.
    /// </summary>
    public const string Llama32_1B = "llama3.2:1b";

    /// <summary>The image's baked-in default, used on machines with memory to spare.</summary>
    public const string Llama32 = "llama3.2";
}
