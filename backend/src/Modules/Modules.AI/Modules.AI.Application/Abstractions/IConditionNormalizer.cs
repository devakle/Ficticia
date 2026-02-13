using Modules.AI.Application.Abstractions.Models;

namespace Modules.AI.Application.Abstractions;

public interface IConditionNormalizer
{
    Task<NormalizedCondition> NormalizeAsync(string text, CancellationToken ct);
}
