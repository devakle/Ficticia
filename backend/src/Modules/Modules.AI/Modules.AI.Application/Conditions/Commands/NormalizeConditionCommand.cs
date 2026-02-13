using BuildingBlocks.Abstractions.Common;
using MediatR;
using Modules.AI.Contracts.Dtos;

namespace Modules.AI.Application.Conditions.Commands;

public sealed record NormalizeConditionCommand(string Text)
    : IRequest<Result<NormalizeConditionResponseDto>>;
