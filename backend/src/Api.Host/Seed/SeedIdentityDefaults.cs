using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Modules.Identity.Infrastructure.Persistence;

namespace Api.Host.Seed;

public static class SeedIdentityDefaults
{
    public static async Task SeedAsync(IServiceProvider sp)
    {
        using var scope = sp.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Seed.Identity");

        logger.LogInformation("IDENTITY SEED START");

        var cs = db.Database.GetConnectionString();

        await SqlServerStartup.WaitUntilServerReadyAsync(cs, "IdentityDb", logger);

        try
        {
            await SqlServerStartup.RunWithDatabaseLockAsync(
                cs,
                "IdentityDb",
                logger,
                async () =>
                {
                    await SqlServerStartup.EnsureDatabaseExistsAsync(cs, "IdentityDb", logger);

                    var pending = await SqlServerStartup.GetPendingMigrationsWithRetriesAsync(
                        () => db.Database.GetPendingMigrationsAsync(),
                        "IdentityDb",
                        logger);

                    if (pending.Count > 0)
                    {
                        await SqlServerStartup.MigrateWithRetriesAsync(
                            () => db.Database.MigrateAsync(),
                            "IdentityDb",
                            logger);
                    }
                    else
                    {
                        logger.LogInformation("IDENTITY MIGRATION SKIP: no pending migrations");
                    }
                });
        }
        catch (Exception ex) when (SqlServerStartup.IsDatabaseAlreadyExists(ex))
        {
            logger.LogWarning(ex, "IDENTITY MIGRATION CONTINUE: database already exists");
        }

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        var roles = new[] { "Admin", "Manager", "Viewer" };

        foreach (var r in roles)
        {
            if (!await roleManager.RoleExistsAsync(r))
            {
                await roleManager.CreateAsync(new IdentityRole(r));
            }
        }

        var adminEmail = "admin@ficticia.local";
        var adminPass = "Admin123!";

        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin is null)
        {
            admin = new IdentityUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };

            var created = await userManager.CreateAsync(admin, adminPass);
            if (!created.Succeeded)
            {
                throw new Exception("Failed creating admin: " + string.Join("; ", created.Errors.Select(e => e.Description)));
            }

            await userManager.AddToRoleAsync(admin, "Admin");
            logger.LogInformation("IDENTITY SEED DONE: default admin user created");
        }
        else
        {
            logger.LogInformation("IDENTITY SEED SKIP: default admin already exists");
        }
    }
}
