namespace Modules.People.Contracts.Dtos;

public sealed record PersonDto(
    Guid Id,
    string FullName,
    string IdentificationNumber,
    int Age,
    int Gender,
    bool IsActive
);
