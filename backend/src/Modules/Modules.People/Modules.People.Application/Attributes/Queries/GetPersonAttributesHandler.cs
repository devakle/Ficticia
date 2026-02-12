using BuildingBlocks.Abstractions.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Modules.People.Application.Abstractions;
using Modules.People.Contracts.Dtos;

namespace Modules.People.Application.Attributes.Queries;

internal sealed class GetPersonAttributesHandler
    : IRequestHandler<GetPersonAttributesQuery, Result<IReadOnlyList<PersonAttributeDto>>>
{
    private readonly IPersonRepository _people;
    private readonly IAttributeDefinitionRepository _defs;
    private readonly IPersonAttributeRepository _vals;

    public GetPersonAttributesHandler(
        IPersonRepository people,
        IAttributeDefinitionRepository defs,
        IPersonAttributeRepository vals)
    {
        _people = people;
        _defs = defs;
        _vals = vals;
    }

    public async Task<Result<IReadOnlyList<PersonAttributeDto>>> Handle(GetPersonAttributesQuery req, CancellationToken ct)
    {
        // validar existencia persona (evita devolver lista vacía engañosa)
        var exists = await _people.Query().AsNoTracking().AnyAsync(x => x.Id == req.PersonId, ct);
        if (!exists)
            return Result<IReadOnlyList<PersonAttributeDto>>.Fail(PeopleErrors.NotFound, "Persona no encontrada");

        // join defs + values
        var query =
            from v in _vals.Query().AsNoTracking()
            join d in _defs.Query().AsNoTracking()
                on v.AttributeDefinitionId equals d.Id
            where v.PersonId == req.PersonId
            orderby d.Key
            select new PersonAttributeDto(
                d.Key,
                d.DisplayName,
                (int)d.DataType,
                v.ValueBool,
                v.ValueString,
                v.ValueNumber,
                v.ValueDate,
                v.UpdatedAt
            );

        var items = await query.ToListAsync(ct);
        return Result<IReadOnlyList<PersonAttributeDto>>.Ok(items);
    }
}
