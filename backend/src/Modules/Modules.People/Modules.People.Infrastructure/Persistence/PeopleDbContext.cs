using Microsoft.EntityFrameworkCore;
using Modules.People.Domain.Entities;
using Modules.People.Infrastructure.Persistence.Configurations;

namespace Modules.People.Infrastructure.Persistence;

public sealed class PeopleDbContext : DbContext
{
    public DbSet<Person> People => Set<Person>();
    public DbSet<AttributeDefinition> AttributeDefinitions => Set<AttributeDefinition>();
    public DbSet<PersonAttributeValue> PersonAttributeValues => Set<PersonAttributeValue>();

    public PeopleDbContext(DbContextOptions<PeopleDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new PersonConfig());
        modelBuilder.ApplyConfiguration(new AttributeDefinitionConfig());
        modelBuilder.ApplyConfiguration(new PersonAttributeValueConfig());
    }
}
