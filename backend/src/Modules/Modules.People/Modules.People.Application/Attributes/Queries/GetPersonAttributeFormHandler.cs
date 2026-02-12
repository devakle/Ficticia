using BuildingBlocks.Abstractions.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Modules.People.Application.Abstractions;
using Modules.People.Contracts.Dtos;

namespace Modules.People.Application.Attributes.Queries;

internal sealed class GetPersonAttributeFormHandler
    : IRequestHandler<GetPersonAttributeFormQuery, Result<IReadOnlyList<PersonAttributeFormItemDto>>>
{
    private readonly IPersonRepository _people;
    private readonly IAttributeDefinitionRepository _defs;
    private readonly IPersonAttributeRepository _vals;

    public GetPersonAttributeFormHandler(
        IPersonRepository people,
        IAttributeDefinitionRepository defs,
        IPersonAttributeRepository vals)
    {
        _people = people;
        _defs = defs;
        _vals = vals;
    }

    public async Task<Result<IReadOnlyList<PersonAttributeFormItemDto>>> Handle(GetPersonAttributeFormQuery req, CancellationToken ct)
    {
        // 1) validar existencia persona
        var exists = await _people.Query().AsNoTracking().AnyAsync(x => x.Id == req.PersonId, ct);
        if (!exists)
            return Result<IReadOnlyList<PersonAttributeFormItemDto>>.Fail(PeopleErrors.NotFound, "Persona no encontrada");

        // 2) defs (catálogo)
        var defsQuery = _defs.Query().AsNoTracking();
        if (req.OnlyActive) defsQuery = defsQuery.Where(d => d.IsActive);

        // 3) LEFT JOIN defs -> values (por persona)
        var query =
            from d in defsQuery
            join v in _vals.Query().AsNoTracking().Where(x => x.PersonId == req.PersonId)
                on d.Id equals v.AttributeDefinitionId into gj
            from v in gj.DefaultIfEmpty()
            orderby d.Key
            select new PersonAttributeFormItemDto(
                d.Key,
                d.DisplayName,
                (int)d.DataType,
                d.IsFilterable,
                d.IsActive,
                d.ValidationRulesJson,
                v != null ? v.ValueBool : null,
                v != null ? v.ValueString : null,
                v != null ? v.ValueNumber : null,
                v != null ? v.ValueDate : null,
                v != null ? v.UpdatedAt : null
            );

        var items = await query.ToListAsync(ct);
        return Result<IReadOnlyList<PersonAttributeFormItemDto>>.Ok(items);
    }
}
