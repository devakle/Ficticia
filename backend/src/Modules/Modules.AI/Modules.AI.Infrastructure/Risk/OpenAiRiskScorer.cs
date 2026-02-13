using System.Text.Json;
using Microsoft.Extensions.Options;
using Modules.AI.Application.Abstractions;
using Modules.AI.Application.Abstractions.Models;
using Modules.AI.Contracts.Dtos;
using Modules.AI.Infrastructure.OpenAI;

namespace Modules.AI.Infrastructure.Risk;

public sealed class OpenAiRiskScorer : IRiskScorer
{
    private readonly OpenAiClient _client;
    private readonly OpenAiOptions _opt;
    private readonly IPersonFeatureProvider _features;

    public OpenAiRiskScorer(OpenAiClient client, IOptions<OpenAiOptions> opt, IPersonFeatureProvider features)
    {
        _client = client;
        _opt = opt.Value;
        _features = features;
    }

    public async Task<RiskScore> ScorePersonAsync(Guid personId, CancellationToken ct)
    {
        var f = await _features.GetRiskFeaturesAsync(personId, ct);

        // Schema for scoring
        var schema = new
        {
            type = "object",
            additionalProperties = false,
            required = new[] { "score", "band", "reasons" },
            properties = new
            {
                score = new { type = "integer", minimum = 0, maximum = 100 },
                band = new { type = "string", @enum = new[] { "Low", "Medium", "High" } },
                reasons = new { type = "array", items = new { type = "string" }, maxItems = 6 }
            }
        };

        // We reuse OpenAiClient but you can add a generic method CreateJsonAsync(schema,...)
        var resultJson = await _client.CreateJsonAsync(
            model: _opt.Model,
            system: "You are an insurance risk assistant. Score risk from provided structured features. " +
                    "Be conservative. Output must match JSON schema. Reasons must reference only the given features.",
            user: JsonSerializer.Serialize(f),
            schemaName: "risk_score",
            schema: schema,
            ct: ct
        );

        var parsed = JsonSerializer.Deserialize<RiskScoreOpenAi>(resultJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                     ?? throw new InvalidOperationException("Invalid AI response");

        var band = parsed.Band switch
        {
            "Low" => RiskBand.Low,
            "Medium" => RiskBand.Medium,
            "High" => RiskBand.High,
            _ => RiskBand.Medium
        };

        return new RiskScore(parsed.Score, band, parsed.Reasons ?? new List<string>());
    }

    private sealed record RiskScoreOpenAi(int Score, string Band, List<string>? Reasons);
}
