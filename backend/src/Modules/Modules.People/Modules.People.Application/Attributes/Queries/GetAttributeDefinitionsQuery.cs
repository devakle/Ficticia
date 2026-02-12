using BuildingBlocks.Abstractions.Common;
using MediatR;
using Modules.People.Contracts.Dtos;

namespace Modules.People.Application.Attributes.Queries;

public sealed record GetAttributeDefinitionsQuery(bool OnlyActive = true)
    : IRequest<Result<IReadOnlyList<AttributeDefinitionDto>>>;
