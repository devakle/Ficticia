using BuildingBlocks.Abstractions.Common;
using MediatR;
using Modules.People.Contracts.Dtos;

namespace Modules.People.Application.Attributes.Commands;

public sealed record UpsertPersonAttributesCommand(
    Guid PersonId,
    IReadOnlyList<UpsertAttributeValueDto> Values
) : IRequest<Result>;
