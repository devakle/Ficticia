using BuildingBlocks.Abstractions.Common;
using MediatR;
using Modules.People.Contracts.Dtos;

namespace Modules.People.Application.People.Queries;

public sealed record SearchPeopleQuery(SearchPeopleRequest Request)
    : IRequest<Result<PagedResult<PersonDto>>>;
