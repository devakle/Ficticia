namespace Modules.People.Contracts.Dtos;

public sealed record SearchPeopleRequest(
    string? Name,
    string? IdentificationNumber,
    bool? IsActive,
    int? MinAge,
    int? MaxAge,
    IReadOnlyDictionary<string, string>? DynamicFilters,
    int Page = 1,
    int PageSize = 20
);
