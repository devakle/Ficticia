using System.Net;
using System.Net.Http.Json;
using Api.IntegrationTests.Fixtures;
using FluentAssertions;
using Xunit;

[Collection("integration")]
public sealed class AuthLoginTests
{
    private readonly HttpClient _client;

    public AuthLoginTests(MsSqlContainerFixture sql)
    {
        var factory = new CustomWebApplicationFactory(sql.ConnectionString);
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_should_return_token_for_seeded_admin()
    {
        var resp = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "admin@ficticia.local",
            password = "Admin123!"
        });

        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await resp.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body.Should().NotBeNull();
        body!.ContainsKey("access_token").Should().BeTrue();
    }

    [Fact]
    public async Task Login_should_return_unauthorized_for_unknown_email()
    {
        var resp = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "nobody@ficticia.local",
            password = "Admin123!"
        });

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_should_return_unauthorized_for_wrong_password()
    {
        var resp = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "admin@ficticia.local",
            password = "wrong-password"
        });

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
