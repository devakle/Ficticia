using BuildingBlocks.Abstractions.Common;
using MediatR;
using Modules.People.Contracts.Dtos;

namespace Modules.People.Application.Attributes.Commands;

public sealed record CreateAttributeDefinitionCommand(
    string Key,
    string DisplayName,
    int DataType,
    bool IsFilterable,
    string? ValidationRulesJson
) : IRequest<Result<AttributeDefinitionDto>>;
