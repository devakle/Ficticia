using BuildingBlocks.Abstractions.Common;
using BuildingBlocks.Abstractions.Persistence;
using MediatR;
using Modules.People.Application.Abstractions;
using Modules.People.Domain.Enums;

namespace Modules.People.Application.People.Commands;

internal sealed class UpdatePersonHandler : IRequestHandler<UpdatePersonCommand, Result>
{
    private readonly IPersonRepository _people;
    private readonly IUnitOfWork _uow;

    public UpdatePersonHandler(IPersonRepository people, IUnitOfWork uow)
    {
        _people = people;
        _uow = uow;
    }

    public async Task<Result> Handle(UpdatePersonCommand req, CancellationToken ct)
    {
        var person = await _people.GetByIdAsync(req.Id, ct);
        if (person is null) return Result.Fail(PeopleErrors.NotFound, "Persona no encontrada");

        person.Update(req.FullName, req.IdentificationNumber, req.Age, (Gender)req.Gender);

        _people.Update(person);
        await _uow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
