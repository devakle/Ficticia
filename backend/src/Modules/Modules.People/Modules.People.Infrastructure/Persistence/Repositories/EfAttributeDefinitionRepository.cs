using Microsoft.EntityFrameworkCore;
using Modules.People.Application.Abstractions;
using Modules.People.Domain.Entities;
using Modules.People.Infrastructure.Persistence;

namespace Modules.People.Infrastructure.Persistence.Repositories;

internal sealed class EfAttributeDefinitionRepository : IAttributeDefinitionRepository
{
    private readonly PeopleDbContext _db;
    public EfAttributeDefinitionRepository(PeopleDbContext db) => _db = db;

    public Task<AttributeDefinition?> GetByIdAsync(Guid id, CancellationToken ct)
        => _db.AttributeDefinitions.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<AttributeDefinition?> GetByKeyAsync(string key, CancellationToken ct)
        => _db.AttributeDefinitions.FirstOrDefaultAsync(x => x.Key == key.ToLowerInvariant(), ct);

    public Task AddAsync(AttributeDefinition def, CancellationToken ct)
        => _db.AttributeDefinitions.AddAsync(def, ct).AsTask();

    public void Update(AttributeDefinition def) => _db.AttributeDefinitions.Update(def);

    public IQueryable<AttributeDefinition> Query() => _db.AttributeDefinitions.AsQueryable();
}
