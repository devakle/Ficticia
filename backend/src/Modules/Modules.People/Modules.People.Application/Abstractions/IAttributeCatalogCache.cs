using Modules.People.Contracts.Dtos;

namespace Modules.People.Application.Abstractions;

public interface IAttributeCatalogCache
{
    Task<IReadOnlyList<AttributeDefinitionDto>> GetAsync(bool onlyActive, CancellationToken ct);
    Task InvalidateAsync(CancellationToken ct);
}
