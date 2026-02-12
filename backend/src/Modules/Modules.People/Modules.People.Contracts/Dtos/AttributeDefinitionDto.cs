namespace Modules.People.Contracts.Dtos;

public sealed record AttributeDefinitionDto(
    Guid Id,
    string Key,
    string DisplayName,
    int DataType,
    bool IsFilterable,
    bool IsActive,
    string? ValidationRulesJson
);
