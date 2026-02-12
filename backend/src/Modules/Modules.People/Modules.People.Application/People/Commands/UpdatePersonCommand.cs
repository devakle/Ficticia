using BuildingBlocks.Abstractions.Common;
using MediatR;

namespace Modules.People.Application.People.Commands;

public sealed record UpdatePersonCommand(
    Guid Id,
    string FullName,
    string IdentificationNumber,
    int Age,
    int Gender
) : IRequest<Result>;
