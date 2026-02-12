namespace Modules.People.Contracts.Dtos;

public sealed record PersonAttributeDto(
    string Key,
    string DisplayName,
    int DataType,
    bool? BoolValue,
    string? StringValue,
    decimal? NumberValue,
    DateTime? DateValue,
    DateTimeOffset UpdatedAt
);
