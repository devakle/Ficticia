using BuildingBlocks.Abstractions.Common;
using MediatR;

namespace Modules.People.Application.Attributes.Commands;

public sealed record UpdateAttributeDefinitionCommand(
    Guid Id,
    string DisplayName,
    bool IsFilterable,
    bool IsActive,
    string? ValidationRulesJson
) : IRequest<Result>;
