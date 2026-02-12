using BuildingBlocks.Abstractions.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Modules.People.Application.Abstractions;
using Modules.People.Contracts.Dtos;

namespace Modules.People.Application.People.Queries;

internal sealed class SearchPeopleHandler : IRequestHandler<SearchPeopleQuery, Result<PagedResult<PersonDto>>>
{
    private readonly IPersonRepository _people;
    private readonly IAttributeDefinitionRepository _defs;
    private readonly IPersonAttributeRepository _vals;

    public SearchPeopleHandler(IPersonRepository people, IAttributeDefinitionRepository defs, IPersonAttributeRepository vals)
    {
        _people = people;
        _defs = defs;
        _vals = vals;
    }

    public async Task<Result<PagedResult<PersonDto>>> Handle(SearchPeopleQuery q, CancellationToken ct)
    {
        var r = q.Request;

        var query = _people.Query().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(r.Name))
            query = query.Where(x => x.FullName.Contains(r.Name));

        if (!string.IsNullOrWhiteSpace(r.IdentificationNumber))
            query = query.Where(x => x.IdentificationNumber == r.IdentificationNumber);

        if (r.IsActive is not null)
            query = query.Where(x => x.IsActive == r.IsActive);

        if (r.MinAge is not null) query = query.Where(x => x.Age >= r.MinAge);
        if (r.MaxAge is not null) query = query.Where(x => x.Age <= r.MaxAge);

        if (r.DynamicFilters is { Count: > 0 })
        {
            var keys = r.DynamicFilters.Keys
                .Select(Modules.People.Domain.Entities.AttributeDefinition.NormalizeKey)
                .Distinct()
                .ToArray();

            var defs = await _defs.Query().AsNoTracking()
                .Where(d => keys.Contains(d.Key) && d.IsActive && d.IsFilterable)
                .Select(d => new { d.Id, d.Key, d.DataType })
                .ToListAsync(ct);

            var defMap = defs.ToDictionary(x => x.Key, x => x);
            
            var values = _vals.Query().AsNoTracking();

            foreach (var (rawKey, rawVal) in r.DynamicFilters)
            {
                var key = Modules.People.Domain.Entities.AttributeDefinition.NormalizeKey(rawKey);

                if (!defMap.TryGetValue(key, out var def))
                    return Result<PagedResult<PersonDto>>.Fail(PeopleErrors.InvalidFilter, $"Filtro inválido: {key}");

                var v = (rawVal ?? "").Trim();

                switch ((int)def.DataType)
                {
                    case 1: // bool
                        if (!bool.TryParse(v, out var bv))
                            return Result<PagedResult<PersonDto>>.Fail(PeopleErrors.InvalidFilter, $"Filtro {key} requiere boolean");

                        query = query.Where(p =>
                            values.Any(av =>
                                av.PersonId == p.Id &&
                                av.AttributeDefinitionId == def.Id &&
                                av.ValueBool == bv));
                        break;

                    case 2: // string
                    case 5: // enum string
                        if (v.Length == 0) break;

                        // Mejor que Contains (por collation + Like). OJO: %v% sigue sin índice normal.
                        query = query.Where(p =>
                            values.Any(av =>
                                av.PersonId == p.Id &&
                                av.AttributeDefinitionId == def.Id &&
                                av.ValueString != null &&
                                EF.Functions.Like(
                                    EF.Functions.Collate(av.ValueString, "Latin1_General_CI_AI"),
                                    $"%{v}%"
                                )));
                        break;

                    case 3: // number
                        if (!decimal.TryParse(v, out var nv))
                            return Result<PagedResult<PersonDto>>.Fail(PeopleErrors.InvalidFilter, $"Filtro {key} requiere número");

                        query = query.Where(p =>
                            values.Any(av =>
                                av.PersonId == p.Id &&
                                av.AttributeDefinitionId == def.Id &&
                                av.ValueNumber == nv));
                        break;

                    case 4: // date
                        if (!DateTime.TryParse(v, out var dv))
                            return Result<PagedResult<PersonDto>>.Fail(PeopleErrors.InvalidFilter, $"Filtro {key} requiere fecha");

                        var date = dv.Date;

                        query = query.Where(p =>
                            values.Any(av =>
                                av.PersonId == p.Id &&
                                av.AttributeDefinitionId == def.Id &&
                                av.ValueDate == date));
                        break;

                    default:
                        return Result<PagedResult<PersonDto>>.Fail(PeopleErrors.UnsupportedType, $"Tipo no soportado en filtro: {key}");
                }
            }

            query = query.Distinct();
        }

        var page = Math.Max(1, r.Page);
        var pageSize = Math.Clamp(r.PageSize, 1, 100);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(x => x.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new PersonDto(x.Id, x.FullName, x.IdentificationNumber, x.Age, (int)x.Gender, x.IsActive))
            .ToListAsync(ct);

        return Result<PagedResult<PersonDto>>.Ok(new PagedResult<PersonDto>(items, total, page, pageSize));
    }
}
