using BuildingBlocks.Abstractions.Security;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace BuildingBlocks.Infrastructure.Security;

public sealed class HttpUserContext : IUserContext
{
    public string? UserId { get; }
    public string? UserName { get; }
    public IReadOnlyCollection<string> Roles { get; }

    public HttpUserContext(IHttpContextAccessor accessor)
    {
        var user = accessor.HttpContext?.User;

        UserId = user?.FindFirstValue(ClaimTypes.NameIdentifier);
        UserName = user?.Identity?.Name;

        Roles = user?
            .FindAll(ClaimTypes.Role)
            .Select(c => c.Value)
            .ToArray()
            ?? Array.Empty<string>();
    }
}
