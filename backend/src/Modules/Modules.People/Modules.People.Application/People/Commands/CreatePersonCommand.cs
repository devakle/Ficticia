using BuildingBlocks.Abstractions.Common;
using MediatR;
using Modules.People.Contracts.Dtos;

namespace Modules.People.Application.People.Commands;

public sealed record CreatePersonCommand(
    string FullName,
    string IdentificationNumber,
    int Age,
    int Gender
) : IRequest<Result<PersonDto>>;
