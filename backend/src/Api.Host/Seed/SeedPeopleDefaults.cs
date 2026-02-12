using Microsoft.EntityFrameworkCore;
using Modules.People.Domain.Entities;
using Modules.People.Domain.Enums;
using Modules.People.Infrastructure.Persistence;

namespace Api.Host.Seed;

public static class SeedPeopleDefaults
{
    public static async Task SeedAsync(IServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PeopleDbContext>();

        await db.Database.MigrateAsync();

        if (await db.AttributeDefinitions.AnyAsync()) return;

        db.AttributeDefinitions.AddRange(
            new AttributeDefinition("drives", "¿Maneja?", AttributeDataType.Boolean, true, null),
            new AttributeDefinition("uses_glasses", "¿Usa lentes?", AttributeDataType.Boolean, true, null),
            new AttributeDefinition("diabetic", "¿Es diabético?", AttributeDataType.Boolean, true, null),
            new AttributeDefinition("disease_text", "¿Padece alguna otra enfermedad? ¿Cuál?", AttributeDataType.String, true, null)
        );

        await db.SaveChangesAsync();
    }
}
