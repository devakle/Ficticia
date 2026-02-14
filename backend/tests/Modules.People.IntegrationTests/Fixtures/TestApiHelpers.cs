using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Api.IntegrationTests.Fixtures;

internal static class TestApiHelpers
{
    public static async Task<string> LoginAsync(this HttpClient client, string email, string password)
    {
        var resp = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email,
            password
        });

        resp.EnsureSuccessStatusCode();

        var body = await resp.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();
        body.Should().NotBeNull();
        body!.Should().ContainKey("access_token");
        return body["access_token"].GetString()!;
    }

    public static async Task<string> LoginAsAdminAsync(this HttpClient client)
    {
        return await client.LoginAsync("admin@ficticia.local", "Admin123!");
    }

    public static async Task<string> CreateAndLoginAsRoleAsync(
        this CustomWebApplicationFactory factory,
        HttpClient client,
        string role)
    {
        const string password = "RoleUser123";
        var email = $"{role.ToLowerInvariant()}-{Guid.NewGuid():N}@ficticia.local";

        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        if (!await roles.RoleExistsAsync(role))
        {
            var createRole = await roles.CreateAsync(new IdentityRole(role));
            createRole.Succeeded.Should().BeTrue();
        }

        var user = new IdentityUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };

        var createUser = await users.CreateAsync(user, password);
        createUser.Succeeded.Should().BeTrue("test user creation must succeed");

        var addRole = await users.AddToRoleAsync(user, role);
        addRole.Succeeded.Should().BeTrue("role assignment must succeed");

        return await client.LoginAsync(email, password);
    }

    public static void SetBearer(this HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public static async Task<Guid> CreatePersonAsync(
        this HttpClient client,
        string? fullName = null,
        string? identificationNumber = null,
        int age = 35,
        int gender = 1)
    {
        var create = await client.PostAsJsonAsync("/api/v1/people", new
        {
            fullName = fullName ?? $"Person-{Guid.NewGuid():N}",
            identificationNumber = identificationNumber ?? $"ID-{Guid.NewGuid():N}",
            age,
            gender
        });

        create.EnsureSuccessStatusCode();

        var body = await create.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();
        body.Should().NotBeNull();
        body!.Should().ContainKey("id");
        return body["id"].GetGuid();
    }

    public static async Task<JsonDocument> ReadJsonAsync(this HttpResponseMessage response)
    {
        var raw = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(raw);
    }
}
