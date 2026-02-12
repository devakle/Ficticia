using Modules.People.Domain.Entities;

namespace Modules.People.Application.Abstractions;

public interface IPersonAttributeRepository
{
    Task<PersonAttributeValue?> GetAsync(Guid personId, Guid attrId, CancellationToken ct);
    Task AddAsync(PersonAttributeValue value, CancellationToken ct);
    void Update(PersonAttributeValue value);
    IQueryable<PersonAttributeValue> Query();
}
