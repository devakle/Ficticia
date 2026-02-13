using Modules.AI.Contracts.Dtos;

namespace Modules.AI.Application.Abstractions.Models;

public sealed record RiskScore(
    int Score,
    RiskBand Band,
    IReadOnlyList<string> Reasons
);
