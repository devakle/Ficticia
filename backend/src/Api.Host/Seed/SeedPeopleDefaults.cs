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
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Seed.People");

        logger.LogInformation("PEOPLE SEED START");

        var cs = db.Database.GetConnectionString();

        await SqlServerStartup.WaitUntilServerReadyAsync(cs, "PeopleDb", logger);

        try
        {
            await SqlServerStartup.RunWithDatabaseLockAsync(
                cs,
                "PeopleDb",
                logger,
                async () =>
                {
                    await SqlServerStartup.EnsureDatabaseExistsAsync(cs, "PeopleDb", logger);

                    var pending = await SqlServerStartup.GetPendingMigrationsWithRetriesAsync(
                        () => db.Database.GetPendingMigrationsAsync(),
                        "PeopleDb",
                        logger);

                    if (pending.Count > 0)
                    {
                        await SqlServerStartup.MigrateWithRetriesAsync(
                            () => db.Database.MigrateAsync(),
                            "PeopleDb",
                            logger);
                    }
                    else
                    {
                        logger.LogInformation("PEOPLE MIGRATION SKIP: no pending migrations");
                    }
                });
        }
        catch (Exception ex) when (SqlServerStartup.IsDatabaseAlreadyExists(ex))
        {
            logger.LogWarning(ex, "PEOPLE MIGRATION CONTINUE: database already exists");
        }

        if (await db.AttributeDefinitions.AnyAsync())
        {
            logger.LogInformation("PEOPLE SEED SKIP: AttributeDefinitions already exist");
            return;
        }

        db.AttributeDefinitions.AddRange(
            new AttributeDefinition("drives", "¿Maneja?", AttributeDataType.Boolean, true, null),
            new AttributeDefinition("uses_glasses", "¿Usa lentes?", AttributeDataType.Boolean, true, null),
            new AttributeDefinition("diabetic", "¿Es diabético?", AttributeDataType.Boolean, true, null),
            new AttributeDefinition("disease_text", "¿Padece alguna otra enfermedad? ¿Cuál?", AttributeDataType.String, true, null),
            new AttributeDefinition(
                "condition_code",
                "Condición (código)",
                AttributeDataType.Enum,
                true,
                "{ \"allowedValues\": [\"hipertension\",\"diabetes\",\"asma\",\"enfermedad_cardiaca\",\"ninguna\",\"desconocida\"] }"
            )
        );

        await db.SaveChangesAsync();
        logger.LogInformation("PEOPLE SEED DONE: default attribute definitions created");
    }
}
