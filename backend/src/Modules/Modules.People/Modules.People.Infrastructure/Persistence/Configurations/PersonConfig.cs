using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Modules.People.Domain.Entities;

namespace Modules.People.Infrastructure.Persistence.Configurations;

internal sealed class PersonConfig : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> b)
    {
        b.ToTable("People");
        b.HasKey(x => x.Id);

        b.Property(x => x.FullName).HasMaxLength(200).IsRequired();
        b.Property(x => x.IdentificationNumber).HasMaxLength(50).IsRequired();

        b.Property(x => x.Age).IsRequired();
        b.Property(x => x.Gender).HasConversion<int>().IsRequired();
        b.Property(x => x.IsActive).IsRequired();

        b.Property(x => x.CreatedAt).IsRequired();
        b.Property(x => x.UpdatedAt).IsRequired();

        b.HasIndex(x => x.IdentificationNumber).IsUnique();
        b.HasIndex(x => x.IsActive);
        b.HasIndex(x => x.FullName);
    }
}
