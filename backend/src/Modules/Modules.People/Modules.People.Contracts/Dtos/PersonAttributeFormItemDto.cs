namespace Modules.People.Contracts.Dtos;

public sealed record PersonAttributeFormItemDto(
    string Key,
    string DisplayName,
    int DataType,
    bool IsFilterable,
    bool IsActive,
    string? ValidationRulesJson,
    bool? BoolValue,
    string? StringValue,
    decimal? NumberValue,
    DateTime? DateValue,
    DateTimeOffset? UpdatedAt
);
