namespace Modules.AI.Contracts.Dtos;

public sealed record RiskScoreResponseDto(
    int Score,
    RiskBand Band,
    IReadOnlyList<string> Reasons
);
