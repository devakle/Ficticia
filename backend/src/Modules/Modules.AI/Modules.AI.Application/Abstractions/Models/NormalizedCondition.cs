using Modules.People.Contracts.Dtos;

namespace Modules.AI.Application.Abstractions.Models;

public sealed record NormalizedCondition(
    string Code,
    string Label,
    double Confidence,
    IReadOnlyList<string> MatchedTerms,
    IReadOnlyList<UpsertAttributeValueDto> SuggestedAttributes,
    string Source
);
