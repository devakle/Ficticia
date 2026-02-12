using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Modules.People.Domain.Entities;

namespace Modules.People.Infrastructure.Persistence.Configurations;

internal sealed class PersonAttributeValueConfig : IEntityTypeConfiguration<PersonAttributeValue>
{
    public void Configure(EntityTypeBuilder<PersonAttributeValue> b)
    {
        b.ToTable("PersonAttributeValues");
        b.HasKey(x => new { x.PersonId, x.AttributeDefinitionId });

        b.Property(x => x.ValueString).HasMaxLength(500);
        b.Property(x => x.UpdatedAt).IsRequired();

        b.HasIndex(x => new { x.AttributeDefinitionId, x.ValueBool });
        b.HasIndex(x => new { x.AttributeDefinitionId, x.ValueNumber });
        b.HasIndex(x => new { x.AttributeDefinitionId, x.ValueDate });
        b.HasIndex(x => new { x.AttributeDefinitionId, x.ValueString });
    }
}
