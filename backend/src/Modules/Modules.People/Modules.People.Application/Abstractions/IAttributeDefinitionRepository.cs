using Modules.People.Domain.Entities;

namespace Modules.People.Application.Abstractions;

public interface IAttributeDefinitionRepository
{
    Task<AttributeDefinition?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<AttributeDefinition?> GetByKeyAsync(string key, CancellationToken ct);
    Task AddAsync(AttributeDefinition def, CancellationToken ct);
    void Update(AttributeDefinition def);
    IQueryable<AttributeDefinition> Query();
}
