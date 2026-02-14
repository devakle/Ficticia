using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Api.Host.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<object>> Login(
        [FromBody] LoginRequest req,
        [FromServices] UserManager<IdentityUser> users,
        [FromServices] SignInManager<IdentityUser> signIn,
        [FromServices] IConfiguration cfg)
    {
        var user = await users.FindByEmailAsync(req.Email);
        if (user is null) return Unauthorized("Invalid credentials");

        var ok = await signIn.CheckPasswordSignInAsync(user, req.Password, lockoutOnFailure: false);
        if (!ok.Succeeded) return Unauthorized("Invalid credentials");

        var roles = await users.GetRolesAsync(user);

        var jwt = cfg.GetSection("Jwt");
        var jwtIssuer = jwt["Issuer"] ?? "Ficticia.Api";
        var jwtAudience = jwt["Audience"] ?? "Ficticia.Web";
        var expiresMinutes = int.TryParse(jwt["ExpiresMinutes"], out var m) ? m : 60;
        var jwtKey = jwt["Key"] ?? throw new InvalidOperationException("Jwt:Key is required.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? user.Email ?? "")
        };

        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
            signingCredentials: creds
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return Ok(new
        {
            access_token = tokenString,
            token_type = "Bearer",
            expires_in_minutes = expiresMinutes,
            roles
        });
    }
}

public sealed record LoginRequest(string Email, string Password);
