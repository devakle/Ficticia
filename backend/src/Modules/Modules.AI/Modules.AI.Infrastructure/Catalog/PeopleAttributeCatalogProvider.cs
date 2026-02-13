using Microsoft.EntityFrameworkCore;
using Modules.AI.Application.Abstractions;
using Modules.People.Application.Abstractions;

namespace Modules.AI.Infrastructure.Catalog;

public sealed class PeopleAttributeCatalogProvider : IAttributeCatalogProvider
{
    private readonly IAttributeDefinitionRepository _defs;

    public PeopleAttributeCatalogProvider(IAttributeDefinitionRepository defs)
    {
        _defs = defs;
    }

    public async Task<IReadOnlyList<string>> GetAllowedConditionCodesAsync(CancellationToken ct)
    {
        var rulesJson = await _defs.Query().AsNoTracking()
            .Where(d => d.Key == "condition_code" && d.IsActive)
            .Select(d => d.ValidationRulesJson)
            .FirstOrDefaultAsync(ct);

        var rules = ValidationRulesParser.TryParse(rulesJson);

        var allowed = rules?.AllowedValues?
            .Select(x => x.Trim().ToLowerInvariant())
            .Where(x => x.Length > 0)
            .Distinct()
            .ToList();

        // fallback seguro
        return allowed is { Count: > 0 }
            ? allowed
            : new List<string> { "unknown" };
    }
}
