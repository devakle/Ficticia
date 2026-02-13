namespace Modules.AI.Application.Abstractions;

public interface IAttributeCatalogProvider
{
    Task<IReadOnlyList<string>> GetAllowedConditionCodesAsync(CancellationToken ct);
}
