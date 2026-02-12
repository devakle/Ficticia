using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Modules.People.Domain.Entities;

namespace Modules.People.Infrastructure.Persistence.Configurations;

internal sealed class AttributeDefinitionConfig : IEntityTypeConfiguration<AttributeDefinition>
{
    public void Configure(EntityTypeBuilder<AttributeDefinition> b)
    {
        b.ToTable("AttributeDefinitions");
        b.HasKey(x => x.Id);

        b.Property(x => x.Key).HasMaxLength(80).IsRequired();
        b.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
        b.Property(x => x.DataType).HasConversion<int>().IsRequired();

        b.Property(x => x.IsFilterable).IsRequired();
        b.Property(x => x.IsActive).IsRequired();

        b.Property(x => x.ValidationRulesJson).HasMaxLength(2000);

        b.HasIndex(x => x.Key).IsUnique();
        b.HasIndex(x => x.IsActive);
    }
}
