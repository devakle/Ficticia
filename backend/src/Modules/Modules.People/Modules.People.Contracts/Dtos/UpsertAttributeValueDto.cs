namespace Modules.People.Contracts.Dtos;

public sealed record UpsertAttributeValueDto(
    string Key,
    bool? BoolValue,
    string? StringValue,
    decimal? NumberValue,
    DateTime? DateValue
);
