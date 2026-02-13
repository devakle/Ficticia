using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Modules.AI.Application.Abstractions;
using Modules.AI.Application.Abstractions.Models;
using Modules.AI.Infrastructure.Caching;
using Modules.People.Contracts.Dtos;
using System.Text.Json;

namespace Modules.AI.Infrastructure.OpenAI;

public sealed class OpenAiConditionNormalizer : IConditionNormalizer
{
    private readonly OpenAiClient _client;
    private readonly OpenAiOptions _opt;
    private readonly IDistributedCache? _cache;
    private readonly IAttributeCatalogProvider _catalog;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public OpenAiConditionNormalizer(
        OpenAiClient client,
        IOptions<OpenAiOptions> opt,
        IAttributeCatalogProvider catalog,
        IServiceProvider sp)
    {
        _client = client;
        _opt = opt.Value;
        _catalog = catalog;

        // cache opcional: si no tenés Redis configurado, esto queda null y listo
        _cache = sp.GetService(typeof(IDistributedCache)) as IDistributedCache;
    }

    public async Task<NormalizedCondition> NormalizeAsync(string text, CancellationToken ct)
    {
        text = (text ?? "").Trim();
        if (text.Length == 0)
            return new NormalizedCondition("unknown","Unknown",0,Array.Empty<string>(),Array.Empty<UpsertAttributeValueDto>(),"openai");

        var allowedCodes = await _catalog.GetAllowedConditionCodesAsync(ct);

        var ai = await _client.NormalizeConditionAsync(_opt.Model, text, allowedCodes, ct);

        var code = ai.Code.Trim().ToLowerInvariant();
        if (!allowedCodes.Contains(code))
            code = "unknown";

        var suggested = new List<UpsertAttributeValueDto>();
        if (ai.Confidence >= _opt.ConfidenceThreshold && code != "unknown")
            suggested.Add(new UpsertAttributeValueDto("condition_code", null, code, null, null));

        return new NormalizedCondition(code, ai.Label, ai.Confidence, ai.Matched_Terms ?? new(), suggested, "openai");
    }

    private NormalizedCondition DictionaryFallback(string text)
    {
        var t = text.ToLowerInvariant();

        // super simple, pero rinde muchísimo en español
        if (t.Contains("hta") || t.Contains("hipertension") || t.Contains("hipertensión") || t.Contains("presion alta") || t.Contains("presión alta"))
        {
            return new NormalizedCondition(
                Code: "hypertension",
                Label: "Hypertension",
                Confidence: 0.9,
                MatchedTerms: new[] { "hta/hipertensión" },
                SuggestedAttributes: new[]
                {
                    new UpsertAttributeValueDto("condition_code", null, "hypertension", null, null)
                },
                Source: "dictionary"
            );
        }

        if (t.Contains("diabetes"))
        {
            return new NormalizedCondition(
                Code: "diabetes",
                Label: "Diabetes",
                Confidence: 0.9,
                MatchedTerms: new[] { "diabetes" },
                SuggestedAttributes: new[]
                {
                    new UpsertAttributeValueDto("condition_code", null, "diabetes", null, null),
                    new UpsertAttributeValueDto("diabetic", true, null, null, null) // si existe ese atributo en tu catálogo
                },
                Source: "dictionary"
            );
        }

        return new NormalizedCondition(
            Code: "unknown",
            Label: "Unknown",
            Confidence: 0.2,
            MatchedTerms: Array.Empty<string>(),
            SuggestedAttributes: Array.Empty<UpsertAttributeValueDto>(),
            Source: "dictionary"
        );
    }

    private async Task SetCacheIfAny(string text, NormalizedCondition value, CancellationToken ct)
    {
        if (_cache is null) return;

        var key = AiCacheKeys.NormalizeCondition(text);

        await _cache.SetStringAsync(
            key,
            JsonSerializer.Serialize(value, JsonOpts),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(6) },
            ct
        );
    }
}
