using BuildingBlocks.Abstractions.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Modules.People.Application.Abstractions;
using Modules.People.Contracts.Dtos;

namespace Modules.People.Application.People.Queries;

internal sealed class GetPersonByIdHandler : IRequestHandler<GetPersonByIdQuery, Result<PersonDto>>
{
    private readonly IPersonRepository _people;

    public GetPersonByIdHandler(IPersonRepository people) => _people = people;

    public async Task<Result<PersonDto>> Handle(GetPersonByIdQuery req, CancellationToken ct)
    {
        var dto = await _people.Query().AsNoTracking()
            .Where(x => x.Id == req.Id)
            .Select(x => new PersonDto(x.Id, x.FullName, x.IdentificationNumber, x.Age, (int)x.Gender, x.IsActive))
            .FirstOrDefaultAsync(ct);

        return dto is null
            ? Result<PersonDto>.Fail(PeopleErrors.NotFound, "Persona no encontrada")
            : Result<PersonDto>.Ok(dto);
    }
}
