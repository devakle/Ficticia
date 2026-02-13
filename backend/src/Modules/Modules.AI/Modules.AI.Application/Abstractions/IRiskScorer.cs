using Modules.AI.Application.Abstractions.Models;

namespace Modules.AI.Application.Abstractions;

public interface IRiskScorer
{
    Task<RiskScore> ScorePersonAsync(Guid personId, CancellationToken ct);
}
