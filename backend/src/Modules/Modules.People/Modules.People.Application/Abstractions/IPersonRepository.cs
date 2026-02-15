using Modules.People.Domain.Entities;

namespace Modules.People.Application.Abstractions;

public interface IPersonRepository
{
    Task<Person?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<bool> ExistsByIdentificationAsync(string identificationNumber, Guid? excludingPersonId, CancellationToken ct);
    Task AddAsync(Person person, CancellationToken ct);
    void Update(Person person);
    IQueryable<Person> Query();
}
