using BuildingBlocks.Abstractions.Common;
using BuildingBlocks.Abstractions.Persistence;
using MediatR;
using Modules.People.Application.Abstractions;

namespace Modules.People.Application.People.Commands;

internal sealed class SetPersonStatusHandler : IRequestHandler<SetPersonStatusCommand, Result>
{
    private readonly IPersonRepository _people;
    private readonly IUnitOfWork _uow;

    public SetPersonStatusHandler(IPersonRepository people, IUnitOfWork uow)
    {
        _people = people;
        _uow = uow;
    }

    public async Task<Result> Handle(SetPersonStatusCommand req, CancellationToken ct)
    {
        var person = await _people.GetByIdAsync(req.Id, ct);
        if (person is null) return Result.Fail(PeopleErrors.NotFound, "Persona no encontrada");

        person.SetStatus(req.IsActive);
        _people.Update(person);
        await _uow.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
