using Microsoft.EntityFrameworkCore;
using Modules.AI.Application.Abstractions;
using Modules.People.Application.Abstractions;

namespace Modules.AI.Infrastructure.Risk;

public sealed class PeoplePersonFeatureProvider : IPersonFeatureProvider
{
    private readonly IPersonRepository _people;
    private readonly IAttributeDefinitionRepository _defs;
    private readonly IPersonAttributeRepository _vals;

    public PeoplePersonFeatureProvider(IPersonRepository people, IAttributeDefinitionRepository defs, IPersonAttributeRepository vals)
    {
        _people = people; _defs = defs; _vals = vals;
    }

    public async Task<PersonRiskFeatures> GetRiskFeaturesAsync(Guid personId, CancellationToken ct)
    {
        var person = await _people.Query().AsNoTracking().FirstOrDefaultAsync(x => x.Id == personId, ct);
        if (person is null) throw new KeyNotFoundException();

        var keys = new[] { "smoker", "diabetic", "condition_code" };

        var defs = await _defs.Query().AsNoTracking()
            .Where(d => keys.Contains(d.Key) && d.IsActive)
            .Select(d => new { d.Id, d.Key })
            .ToListAsync(ct);

        var map = defs.ToDictionary(x => x.Key, x => x.Id);
        var ids = defs.Select(x => x.Id).ToArray();

        var values = await _vals.Query().AsNoTracking()
            .Where(v => v.PersonId == personId && ids.Contains(v.AttributeDefinitionId))
            .ToListAsync(ct);

        bool? smoker = map.TryGetValue("smoker", out var sid)
            ? values.FirstOrDefault(v => v.AttributeDefinitionId == sid)?.ValueBool
            : null;

        bool? diabetic = map.TryGetValue("diabetic", out var did)
            ? values.FirstOrDefault(v => v.AttributeDefinitionId == did)?.ValueBool
            : null;

        string? conditionCode = map.TryGetValue("condition_code", out var cid)
            ? values.FirstOrDefault(v => v.AttributeDefinitionId == cid)?.ValueString?.Trim().ToLowerInvariant()
            : null;

        return new PersonRiskFeatures(person.Age, (int)person.Gender, person.IsActive, conditionCode, diabetic, smoker);
    }
}
