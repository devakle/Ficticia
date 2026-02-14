using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Api.IntegrationTests.Fixtures;
using FluentAssertions;
using Xunit;

[Collection("integration")]
public sealed class PeopleAttributeUpsertTests
{
    private readonly HttpClient _client;

    public PeopleAttributeUpsertTests(MsSqlContainerFixture sql)
    {
        var factory = new CustomWebApplicationFactory(sql.ConnectionString);
        _client = factory.CreateClient();
    }

    private async Task<string> LoginAsync()
    {
        var resp = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "admin@ficticia.local",
            password = "Admin123!"
        });

        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        return body!["access_token"].ToString()!;
    }

    [Fact]
    public async Task Upsert_should_fail_when_enum_is_not_allowed()
    {
        var token = await LoginAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // 1) Create a person (adjust endpoint/body to your actual People create)
        var create = await _client.PostAsJsonAsync("/api/v1/people", new
        {
            fullName = "Test Person",
            identificationNumber = "ID-123",
            age = 40,
            gender = 1,
            isActive = true
        });
        create.EnsureSuccessStatusCode();

        var created = await create.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        var personId = created!["id"].ToString();

        // 2) Upsert invalid enum value
        var upsert = await _client.PutAsJsonAsync($"/api/v1/people/{personId}/attributes", new[]
        {
            new { key = "condition_code", boolValue = (bool?)null, stringValue = "anything", numberValue = (decimal?)null, dateValue = (string?)null }
        });

        upsert.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
