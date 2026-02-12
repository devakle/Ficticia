using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Modules.Identity.Infrastructure.Persistence;

public sealed class IdentityDbContext : IdentityDbContext<IdentityUser, IdentityRole, string>
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options) { }
}
