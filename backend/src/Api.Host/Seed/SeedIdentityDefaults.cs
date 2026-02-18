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
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

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

                    var roles = new[] { "Admin", "Manager", "Viewer" };

                    foreach (var role in roles)
                    {
                        if (!await roleManager.RoleExistsAsync(role))
                        {
                            await roleManager.CreateAsync(new IdentityRole(role));
                        }
                    }

                    var seedUsers = new[]
                    {
                        new SeedUser("admin@ficticia.local", "Admin123!", "Admin"),
                        new SeedUser("manager@ficticia.local", "Manager123!", "Manager"),
                        new SeedUser("viewer@ficticia.local", "Viewer123!", "Viewer")
                    };

                    foreach (var seedUser in seedUsers)
                    {
                        await EnsureUserWithRoleAsync(userManager, seedUser, logger);
                    }
                });
        }
        catch (Exception ex) when (SqlServerStartup.IsDatabaseAlreadyExists(ex))
        {
            logger.LogWarning(ex, "IDENTITY MIGRATION CONTINUE: database already exists");
        }
    }

    private static async Task EnsureUserWithRoleAsync(
        UserManager<IdentityUser> userManager,
        SeedUser seedUser,
        ILogger logger)
    {
        var user = await userManager.FindByEmailAsync(seedUser.Email);
        if (user is null)
        {
            user = new IdentityUser
            {
                UserName = seedUser.Email,
                Email = seedUser.Email,
                EmailConfirmed = true
            };

            var created = await userManager.CreateAsync(user, seedUser.Password);
            if (!created.Succeeded)
            {
                throw new Exception($"Failed creating {seedUser.Role} user: " + string.Join("; ", created.Errors.Select(e => e.Description)));
            }

            logger.LogInformation("IDENTITY SEED DONE: user {Email} created", seedUser.Email);
        }
        else
        {
            logger.LogInformation("IDENTITY SEED SKIP: user {Email} already exists", seedUser.Email);
        }

        if (!await userManager.IsInRoleAsync(user, seedUser.Role))
        {
            var added = await userManager.AddToRoleAsync(user, seedUser.Role);
            if (!added.Succeeded)
            {
                throw new Exception($"Failed assigning role {seedUser.Role} to {seedUser.Email}: " + string.Join("; ", added.Errors.Select(e => e.Description)));
            }

            logger.LogInformation("IDENTITY SEED DONE: role {Role} assigned to {Email}", seedUser.Role, seedUser.Email);
        }
    }

    private sealed record SeedUser(string Email, string Password, string Role);
}
