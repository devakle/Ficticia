using BuildingBlocks.Abstractions.Common;
using MediatR;
using Modules.AI.Contracts.Dtos;

namespace Modules.AI.Application.PeopleRisk.Commands;

public sealed record ScorePersonRiskCommand(Guid PersonId)
    : IRequest<Result<RiskScoreResponseDto>>;
