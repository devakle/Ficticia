using BuildingBlocks.Abstractions.Common;
using MediatR;
using Modules.AI.Application.Abstractions;
using Modules.AI.Contracts.Dtos;

namespace Modules.AI.Application.PeopleRisk.Commands;

internal sealed class ScorePersonRiskHandler
    : IRequestHandler<ScorePersonRiskCommand, Result<RiskScoreResponseDto>>
{
    private readonly IRiskScorer _scorer;

    public ScorePersonRiskHandler(IRiskScorer scorer)
    {
        _scorer = scorer;
    }

    public async Task<Result<RiskScoreResponseDto>> Handle(ScorePersonRiskCommand req, CancellationToken ct)
    {
        try
        {
            var risk = await _scorer.ScorePersonAsync(req.PersonId, ct);
            return Result<RiskScoreResponseDto>.Ok(new RiskScoreResponseDto(risk.Score, risk.Band, risk.Reasons));
        }
        catch (KeyNotFoundException)
        {
            return Result<RiskScoreResponseDto>.Fail(AiErrors.PersonNotFound, "Persona no encontrada");
        }
        catch (Exception ex)
        {
            return Result<RiskScoreResponseDto>.Fail(AiErrors.ProviderFailed, ex.Message);
        }
    }
}
