using BuildingBlocks.Abstractions.Common;
using MediatR;
using Modules.People.Contracts.Dtos;

namespace Modules.People.Application.Attributes.Queries;

public sealed record GetPersonAttributeFormQuery(Guid PersonId, bool OnlyActive)
    : IRequest<Result<IReadOnlyList<PersonAttributeFormItemDto>>>;
