namespace Modules.AI.Infrastructure.OpenAI;

public sealed class OpenAiOptions
{
    public string ApiKey { get; init; } = default!;
    public string BaseUrl { get; init; } = "https://api.openai.com/v1";
    public string Model { get; init; } = "gpt-4o-mini";

    // si OpenAI no está disponible, devolvemos fallback de diccionario
    public bool EnableFallbackDictionary { get; init; } = true;

    // threshold para auto-sugerir condition_code
    public double ConfidenceThreshold { get; init; } = 0.85;
}
