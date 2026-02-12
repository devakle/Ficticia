using Microsoft.EntityFrameworkCore;
using Modules.People.Application.Abstractions;
using Modules.People.Domain.Entities;
using Modules.People.Infrastructure.Persistence;

namespace Modules.People.Infrastructure.Persistence.Repositories;

internal sealed class EfPersonAttributeRepository : IPersonAttributeRepository
{
    private readonly PeopleDbContext _db;
    public EfPersonAttributeRepository(PeopleDbContext db) => _db = db;

    public Task<PersonAttributeValue?> GetAsync(Guid personId, Guid attrId, CancellationToken ct)
        => _db.PersonAttributeValues.FirstOrDefaultAsync(x => x.PersonId == personId && x.AttributeDefinitionId == attrId, ct);

    public Task AddAsync(PersonAttributeValue value, CancellationToken ct)
        => _db.PersonAttributeValues.AddAsync(value, ct).AsTask();

    public void Update(PersonAttributeValue value) => _db.PersonAttributeValues.Update(value);

    public IQueryable<PersonAttributeValue> Query() => _db.PersonAttributeValues.AsQueryable();
}
