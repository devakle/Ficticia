using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
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

    private async Task<Guid> CreatePersonAsync()
    {
        var create = await _client.PostAsJsonAsync("/api/v1/people", new
        {
            fullName = "Test Person",
            identificationNumber = $"ID-{Guid.NewGuid():N}",
            age = 40,
            gender = 1,
            isActive = true
        });

        create.EnsureSuccessStatusCode();

        var created = await create.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();
        created.Should().NotBeNull();
        created!.Should().ContainKey("id");
        return created["id"].GetGuid();
    }

    private async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        var raw = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(raw);
    }

    [Fact]
    public async Task Upsert_should_fail_when_enum_is_not_allowed()
    {
        var token = await LoginAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var personId = await CreatePersonAsync();

        var upsert = await _client.PutAsJsonAsync($"/api/v1/people/{personId}/attributes", new[]
        {
            new { key = "condition_code", boolValue = (bool?)null, stringValue = "anything", numberValue = (decimal?)null, dateValue = (string?)null }
        });

        upsert.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var body = await ReadJsonAsync(upsert);
        body.RootElement.GetProperty("code").GetString().Should().Be("attributes.rule_failed");
    }

    [Fact]
    public async Task Upsert_should_persist_and_normalize_enum_value()
    {
        var token = await LoginAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var personId = await CreatePersonAsync();

        var upsert = await _client.PutAsJsonAsync($"/api/v1/people/{personId}/attributes", new[]
        {
            new { key = "condition_code", boolValue = (bool?)null, stringValue = "  DiAbEtEs  ", numberValue = (decimal?)null, dateValue = (string?)null }
        });

        upsert.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var get = await _client.GetAsync($"/api/v1/people/{personId}/attributes");
        get.StatusCode.Should().Be(HttpStatusCode.OK);

        using var body = await ReadJsonAsync(get);
        var condition = body.RootElement.EnumerateArray().Single(x => x.GetProperty("key").GetString() == "condition_code");
        condition.GetProperty("stringValue").GetString().Should().Be("diabetes");
    }

    [Fact]
    public async Task Upsert_should_fail_when_more_than_one_value_field_is_sent()
    {
        var token = await LoginAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var personId = await CreatePersonAsync();

        var upsert = await _client.PutAsJsonAsync($"/api/v1/people/{personId}/attributes", new[]
        {
            new { key = "condition_code", boolValue = true, stringValue = "diabetes", numberValue = (decimal?)null, dateValue = (string?)null }
        });

        upsert.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var body = await ReadJsonAsync(upsert);
        body.RootElement.GetProperty("code").GetString().Should().Be("attributes.invalid_shape");
    }

    [Fact]
    public async Task Upsert_should_fail_for_unknown_attribute_key()
    {
        var token = await LoginAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var personId = await CreatePersonAsync();

        var upsert = await _client.PutAsJsonAsync($"/api/v1/people/{personId}/attributes", new[]
        {
            new { key = "unknown_attribute", boolValue = (bool?)null, stringValue = "x", numberValue = (decimal?)null, dateValue = (string?)null }
        });

        upsert.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var body = await ReadJsonAsync(upsert);
        body.RootElement.GetProperty("code").GetString().Should().Be("attributes.invalid_key");
    }

    [Fact]
    public async Task Upsert_should_allow_clearing_existing_value()
    {
        var token = await LoginAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var personId = await CreatePersonAsync();

        var set = await _client.PutAsJsonAsync($"/api/v1/people/{personId}/attributes", new[]
        {
            new { key = "diabetic", boolValue = true, stringValue = (string?)null, numberValue = (decimal?)null, dateValue = (string?)null }
        });
        set.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var clear = await _client.PutAsJsonAsync($"/api/v1/people/{personId}/attributes", new[]
        {
            new { key = "diabetic", boolValue = (bool?)null, stringValue = (string?)null, numberValue = (decimal?)null, dateValue = (string?)null }
        });
        clear.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var get = await _client.GetAsync($"/api/v1/people/{personId}/attributes");
        get.StatusCode.Should().Be(HttpStatusCode.OK);

        using var body = await ReadJsonAsync(get);
        var diabetic = body.RootElement.EnumerateArray().Single(x => x.GetProperty("key").GetString() == "diabetic");
        diabetic.GetProperty("boolValue").ValueKind.Should().Be(JsonValueKind.Null);
        diabetic.GetProperty("stringValue").ValueKind.Should().Be(JsonValueKind.Null);
        diabetic.GetProperty("numberValue").ValueKind.Should().Be(JsonValueKind.Null);
        diabetic.GetProperty("dateValue").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task Upsert_should_fail_when_person_does_not_exist()
    {
        var token = await LoginAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var upsert = await _client.PutAsJsonAsync($"/api/v1/people/{Guid.NewGuid()}/attributes", new[]
        {
            new { key = "diabetic", boolValue = true, stringValue = (string?)null, numberValue = (decimal?)null, dateValue = (string?)null }
        });

        upsert.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var body = await ReadJsonAsync(upsert);
        body.RootElement.GetProperty("code").GetString().Should().Be("people.not_found");
    }
}
