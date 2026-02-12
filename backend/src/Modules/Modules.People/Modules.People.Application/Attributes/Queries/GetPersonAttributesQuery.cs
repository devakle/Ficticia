using BuildingBlocks.Abstractions.Common;
using MediatR;
using Modules.People.Contracts.Dtos;

namespace Modules.People.Application.Attributes.Queries;

public sealed record GetPersonAttributesQuery(Guid PersonId)
    : IRequest<Result<IReadOnlyList<PersonAttributeDto>>>;
