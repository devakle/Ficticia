using System.Net.Http.Json;
using System.Text.Json;

namespace Modules.AI.Infrastructure.OpenAI;

public sealed class OpenAiClient
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public OpenAiClient(HttpClient http) => _http = http;

    public async Task<NormalizeConditionOpenAiResult> NormalizeConditionAsync(
        string model,
        string text,
        IReadOnlyList<string> allowedCodes,
        CancellationToken ct)
    {
        var schema = new
        {
            type = "object",
            additionalProperties = false,
            required = new[] { "code", "label", "confidence", "matched_terms" },
            properties = new
            {
                code = new { type = "string", @enum = allowedCodes },
                label = new { type = "string" },
                confidence = new { type = "number", minimum = 0, maximum = 1 },
                matched_terms = new { type = "array", items = new { type = "string" } }
            }
        };

        var body = new
        {
            model,
            input = new object[]
            {
                new { role = "system", content =
                    "Normalize the condition text into ONE code from the provided list. " +
                    "If not sure, return 'unknown'. Output must match the JSON schema." },
                new { role = "user", content = $"Allowed codes: {string.Join(", ", allowedCodes)}\nText: {text}" }
            },
            text = new
            {
                format = new
                {
                    type = "json_schema",
                    name = "normalize_condition",
                    strict = true,
                    schema
                }
            }
        };


        using var resp = await _http.PostAsJsonAsync("responses", body, ct);
        var raw = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"OpenAI error {(int)resp.StatusCode}: {raw}");

        // Responses API: buscamos el output_text con JSON (lo más robusto en MVP)
        // Estructura puede variar, así que parseamos flexible.
        using var doc = JsonDocument.Parse(raw);

        string? json = ExtractFirstOutputText(doc.RootElement);
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidOperationException("OpenAI response missing output_text");

        var result = JsonSerializer.Deserialize<NormalizeConditionOpenAiResult>(json, JsonOpts);
        if (result is null)
            throw new InvalidOperationException("Failed to deserialize OpenAI JSON");

        return result;
    }

    private static string? ExtractFirstOutputText(JsonElement root)
    {
        // root.output[] -> content[] -> {type:"output_text", text:"..."}
        if (!root.TryGetProperty("output", out var output) || output.ValueKind != JsonValueKind.Array)
            return null;

        foreach (var item in output.EnumerateArray())
        {
            if (!item.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
                continue;

            foreach (var c in content.EnumerateArray())
            {
                if (c.TryGetProperty("type", out var type) && type.GetString() == "output_text" &&
                    c.TryGetProperty("text", out var textEl))
                {
                    return textEl.GetString();
                }
            }
        }

        return null;
    }

    public async Task<string> CreateJsonAsync(
        string model,
        string system,
        string user,
        string schemaName,
        object schema,
    CancellationToken ct)
    {
        var body = new
        {
            model,
            input = new object[]
            {
                new { role = "system", content = system },
                new { role = "user", content = user }
            },
            text = new
            {
                format = new
                {
                    type = "json_schema",
                    name = schemaName,
                    strict = true,
                    schema
                }
            }
        };

        using var resp = await _http.PostAsJsonAsync("responses", body, ct);
        var raw = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"OpenAI error {(int)resp.StatusCode}: {raw}");

        using var doc = JsonDocument.Parse(raw);
        var json = ExtractFirstOutputText(doc.RootElement);
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidOperationException("OpenAI response missing output_text");

        return json!;
    }

}

public sealed record NormalizeConditionOpenAiResult(
    string Code,
    string Label,
    double Confidence,
    List<string> Matched_Terms
);
