using System.Net;
using System.Net.Http.Json;
using Api.IntegrationTests.Fixtures;
using FluentAssertions;
using Xunit;

[Collection("integration")]
public sealed class AttributeDefinitionsTests
{
    private readonly HttpClient _client;

    public AttributeDefinitionsTests(MsSqlContainerFixture sql)
    {
        var factory = new CustomWebApplicationFactory(sql.ConnectionString);
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Attribute_definitions_endpoints_should_require_authentication()
    {
        var resp = await _client.GetAsync("/api/v1/attributes/definitions");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_definitions_should_return_seeded_catalog()
    {
        var token = await _client.LoginAsAdminAsync();
        _client.SetBearer(token);

        var resp = await _client.GetAsync("/api/v1/attributes/definitions?onlyActive=true");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        using var body = await resp.ReadJsonAsync();
        var items = body.RootElement.EnumerateArray().ToList();
        items.Count.Should().BeGreaterThanOrEqualTo(5);
        items.Should().Contain(x => x.GetProperty("key").GetString() == "condition_code");
    }

    [Fact]
    public async Task Create_definition_should_succeed_and_be_retrievable()
    {
        var token = await _client.LoginAsAdminAsync();
        _client.SetBearer(token);

        var key = $"custom_attr_{Guid.NewGuid():N}";

        var create = await _client.PostAsJsonAsync("/api/v1/attributes/definitions", new
        {
            key,
            displayName = "Custom Attr",
            dataType = 2,
            isFilterable = true,
            validationRulesJson = "{ \"maxLength\": 20 }"
        });

        create.StatusCode.Should().Be(HttpStatusCode.OK);

        var all = await _client.GetAsync("/api/v1/attributes/definitions?onlyActive=false");
        all.StatusCode.Should().Be(HttpStatusCode.OK);

        using var body = await all.ReadJsonAsync();
        var items = body.RootElement.EnumerateArray().ToList();
        items.Should().Contain(x => x.GetProperty("key").GetString() == key);
    }

    [Fact]
    public async Task Create_definition_should_fail_for_duplicate_key()
    {
        var token = await _client.LoginAsAdminAsync();
        _client.SetBearer(token);

        var create = await _client.PostAsJsonAsync("/api/v1/attributes/definitions", new
        {
            key = "condition_code",
            displayName = "Duplicated",
            dataType = 5,
            isFilterable = true,
            validationRulesJson = "{ \"allowedValues\": [\"x\"] }"
        });

        create.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var body = await create.ReadJsonAsync();
        body.RootElement.GetProperty("code").GetString().Should().Be("attributes.duplicate");
    }

    [Fact]
    public async Task Create_definition_should_fail_with_validation_error()
    {
        var token = await _client.LoginAsAdminAsync();
        _client.SetBearer(token);

        var create = await _client.PostAsJsonAsync("/api/v1/attributes/definitions", new
        {
            key = "",
            displayName = "",
            dataType = 99,
            isFilterable = true,
            validationRulesJson = (string?)null
        });

        create.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var body = await create.ReadJsonAsync();
        body.RootElement.GetProperty("title").GetString().Should().Be("Validation error");
    }

    [Fact]
    public async Task Update_definition_should_persist_changes()
    {
        var token = await _client.LoginAsAdminAsync();
        _client.SetBearer(token);

        var key = $"upd_attr_{Guid.NewGuid():N}";
        var create = await _client.PostAsJsonAsync("/api/v1/attributes/definitions", new
        {
            key,
            displayName = "To Update",
            dataType = 2,
            isFilterable = true,
            validationRulesJson = "{ \"maxLength\": 20 }"
        });
        create.EnsureSuccessStatusCode();

        using var createBody = await create.ReadJsonAsync();
        var id = createBody.RootElement.GetProperty("id").GetGuid();

        var update = await _client.PutAsJsonAsync($"/api/v1/attributes/definitions/{id}", new
        {
            id,
            displayName = "Updated Display Name",
            isFilterable = false,
            isActive = false,
            validationRulesJson = "{ \"maxLength\": 10 }"
        });
        update.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var all = await _client.GetAsync("/api/v1/attributes/definitions?onlyActive=false");
        all.StatusCode.Should().Be(HttpStatusCode.OK);

        using var allBody = await all.ReadJsonAsync();
        var updated = allBody.RootElement.EnumerateArray().Single(x => x.GetProperty("id").GetGuid() == id);
        updated.GetProperty("displayName").GetString().Should().Be("Updated Display Name");
        updated.GetProperty("isFilterable").GetBoolean().Should().BeFalse();
        updated.GetProperty("isActive").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task Update_definition_should_fail_when_route_and_body_ids_mismatch()
    {
        var token = await _client.LoginAsAdminAsync();
        _client.SetBearer(token);

        var update = await _client.PutAsJsonAsync($"/api/v1/attributes/definitions/{Guid.NewGuid()}", new
        {
            id = Guid.NewGuid(),
            displayName = "Mismatch",
            isFilterable = true,
            isActive = true,
            validationRulesJson = (string?)null
        });

        update.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_definition_should_return_not_found_for_missing_definition()
    {
        var token = await _client.LoginAsAdminAsync();
        _client.SetBearer(token);

        var id = Guid.NewGuid();
        var update = await _client.PutAsJsonAsync($"/api/v1/attributes/definitions/{id}", new
        {
            id,
            displayName = "Missing",
            isFilterable = true,
            isActive = true,
            validationRulesJson = (string?)null
        });

        update.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
