using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Modules.AI.Application.Abstractions;
using Modules.AI.Application.Abstractions.Models;
using Modules.AI.Contracts.Dtos;
using Modules.People.Application.Abstractions;

namespace Modules.AI.Infrastructure.Risk;

public sealed class RulesRiskScorer : IRiskScorer
{
    private readonly IPersonRepository _people;
    private readonly IAttributeDefinitionRepository _defs;
    private readonly IPersonAttributeRepository _vals;
    private readonly RiskRulesOptions _opt;

    public RulesRiskScorer(
        IPersonRepository people,
        IAttributeDefinitionRepository defs,
        IPersonAttributeRepository vals,
        IOptions<RiskRulesOptions> opt)
    {
        _people = people;
        _defs = defs;
        _vals = vals;
        _opt = opt.Value;
    }

    public async Task<RiskScore> ScorePersonAsync(Guid personId, CancellationToken ct)
    {
        var person = await _people.Query().AsNoTracking().FirstOrDefaultAsync(x => x.Id == personId, ct);
        if (person is null) throw new KeyNotFoundException();

        // Traemos defs relevantes por key (evita hardcodear IDs)
        var keys = new[] { "smoker", "diabetic", "condition_code" };
        var defs = await _defs.Query().AsNoTracking()
            .Where(d => keys.Contains(d.Key) && d.IsActive)
            .Select(d => new { d.Id, d.Key })
            .ToListAsync(ct);

        var defMap = defs.ToDictionary(x => x.Key, x => x.Id);

        // Traemos values de esos attrs para esa persona
        var attrIds = defs.Select(x => x.Id).ToArray();

        var values = await _vals.Query().AsNoTracking()
            .Where(v => v.PersonId == personId && attrIds.Contains(v.AttributeDefinitionId))
            .ToListAsync(ct);

        bool smoker = defMap.TryGetValue("smoker", out var smokerId) &&
                      values.Any(v => v.AttributeDefinitionId == smokerId && v.ValueBool == true);

        bool diabetic = defMap.TryGetValue("diabetic", out var diabeticId) &&
                        values.Any(v => v.AttributeDefinitionId == diabeticId && v.ValueBool == true);

        string? conditionCode = null;
        if (defMap.TryGetValue("condition_code", out var condId))
        {
            conditionCode = values
                .Where(v => v.AttributeDefinitionId == condId)
                .Select(v => v.ValueString)
                .FirstOrDefault();
            conditionCode = conditionCode?.Trim().ToLowerInvariant();
        }

        var reasons = new List<string>();
        var score = _opt.BaseScore;

        // Age rules
        if (person.Age is >= 18 and <= 30) { score += _opt.Age_18_30; reasons.Add($"Age 18-30 (+{_opt.Age_18_30})"); }
        else if (person.Age is >= 31 and <= 45) { score += _opt.Age_31_45; reasons.Add($"Age 31-45 (+{_opt.Age_31_45})"); }
        else if (person.Age is >= 46 and <= 60) { score += _opt.Age_46_60; reasons.Add($"Age 46-60 (+{_opt.Age_46_60})"); }
        else if (person.Age >= 61) { score += _opt.Age_61_plus; reasons.Add($"Age 61+ (+{_opt.Age_61_plus})"); }

        if (smoker) { score += _opt.Smoker; reasons.Add($"Smoker (+{_opt.Smoker})"); }
        if (diabetic) { score += _opt.Diabetes; reasons.Add($"Diabetic (+{_opt.Diabetes})"); }

        if (conditionCode == "hypertension") { score += _opt.Hypertension; reasons.Add($"Condition hypertension (+{_opt.Hypertension})"); }
        if (conditionCode == "heart_disease") { score += _opt.HeartDisease; reasons.Add($"Condition heart_disease (+{_opt.HeartDisease})"); }
        if (conditionCode == "diabetes" && !diabetic) { score += 10; reasons.Add("Condition diabetes mentioned (+10)"); }

        score = Math.Clamp(score, 0, 100);

        var band = score <= _opt.LowMax ? RiskBand.Low
                 : score <= _opt.MediumMax ? RiskBand.Medium
                 : RiskBand.High;

        return new RiskScore(score, band, reasons);
    }
}
