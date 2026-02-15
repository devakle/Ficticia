using Microsoft.EntityFrameworkCore;
using Modules.People.Application.Abstractions;
using Modules.People.Domain.Entities;
using Modules.People.Infrastructure.Persistence;

namespace Modules.People.Infrastructure.Persistence.Repositories;

internal sealed class EfPersonRepository : IPersonRepository
{
    private readonly PeopleDbContext _db;
    public EfPersonRepository(PeopleDbContext db) => _db = db;

    public Task<Person?> GetByIdAsync(Guid id, CancellationToken ct)
        => _db.People.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<bool> ExistsByIdentificationAsync(string identificationNumber, Guid? excludingPersonId, CancellationToken ct)
        => _db.People.AnyAsync(
            x => x.IdentificationNumber == identificationNumber && (!excludingPersonId.HasValue || x.Id != excludingPersonId.Value),
            ct);

    public Task AddAsync(Person person, CancellationToken ct)
        => _db.People.AddAsync(person, ct).AsTask();

    public void Update(Person person) => _db.People.Update(person);

    public IQueryable<Person> Query() => _db.People.AsQueryable();
}
