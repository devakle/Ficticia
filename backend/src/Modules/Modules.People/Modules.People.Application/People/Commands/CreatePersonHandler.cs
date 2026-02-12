using BuildingBlocks.Abstractions.Common;
using BuildingBlocks.Abstractions.Persistence;
using MediatR;
using Modules.People.Application.Abstractions;
using Modules.People.Contracts.Dtos;
using Modules.People.Domain.Entities;
using Modules.People.Domain.Enums;

namespace Modules.People.Application.People.Commands;

internal sealed class CreatePersonHandler : IRequestHandler<CreatePersonCommand, Result<PersonDto>>
{
    private readonly IPersonRepository _people;
    private readonly IUnitOfWork _uow;

    public CreatePersonHandler(IPersonRepository people, IUnitOfWork uow)
    {
        _people = people;
        _uow = uow;
    }

    public async Task<Result<PersonDto>> Handle(CreatePersonCommand req, CancellationToken ct)
    {
        var person = new Person(req.FullName, req.IdentificationNumber, req.Age, (Gender)req.Gender);

        await _people.AddAsync(person, ct);
        await _uow.SaveChangesAsync(ct);

        return Result<PersonDto>.Ok(new PersonDto(
            person.Id, person.FullName, person.IdentificationNumber, person.Age, (int)person.Gender, person.IsActive
        ));
    }
}
