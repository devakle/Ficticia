using Modules.People.Contracts.Dtos;

namespace Modules.AI.Contracts.Dtos;

public sealed record NormalizeConditionResponseDto(
    string Code,
    string Label,
    double Confidence,
    IReadOnlyList<string> MatchedTerms,
    IReadOnlyList<UpsertAttributeValueDto> SuggestedAttributes,
    string Source // "dictionary" | "openai"
);
