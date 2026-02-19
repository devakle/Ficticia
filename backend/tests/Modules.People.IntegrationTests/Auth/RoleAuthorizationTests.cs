using System.Net;
using System.Net.Http.Json;
using Api.IntegrationTests.Fixtures;
using FluentAssertions;
using Modules.People.Contracts.Dtos;
using Xunit;

[Collection("integration")]
public sealed class RoleAuthorizationTests
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public RoleAuthorizationTests(MsSqlContainerFixture sql)
    {
        _factory = new CustomWebApplicationFactory(sql.ConnectionString);
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Viewer_should_access_people_read_endpoints()
    {
        var token = await _factory.CreateAndLoginAsRoleAsync(_client, "Viewer");
        _client.SetBearer(token);

        var resp = await _client.GetAsync("/api/v1/people");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Viewer_should_not_access_people_write_endpoints()
    {
        var token = await _factory.CreateAndLoginAsRoleAsync(_client, "Viewer");
        _client.SetBearer(token);

        var resp = await _client.PostAsJsonAsync("/api/v1/people", new
        {
            fullName = "Viewer Cannot Create",
            identificationNumber = $"VIEW-{Guid.NewGuid():N}",
            age = 30,
            gender = 1
        });

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Viewer_should_not_update_person_or_attributes()
    {
        var personId = await CreatePersonAsAdminAsync();

        var viewerToken = await _factory.CreateAndLoginAsRoleAsync(_client, "Viewer");
        _client.SetBearer(viewerToken);

        var updateResp = await _client.PutAsJsonAsync($"/api/v1/people/{personId}", new
        {
            id = personId,
            fullName = "Viewer Cannot Update",
            identificationNumber = $"VIEW-UPD-{Guid.NewGuid():N}",
            age = 33,
            gender = 1,
            isActive = true
        });
        updateResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var statusResp = await _client.PatchAsJsonAsync($"/api/v1/people/{personId}/status", new
        {
            id = personId,
            isActive = false
        });
        statusResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var attrResp = await _client.PutAsJsonAsync($"/api/v1/people/{personId}/attributes", new[]
        {
            new
            {
                key = "condition_code",
                boolValue = (bool?)null,
                stringValue = "diabetes",
                numberValue = (double?)null,
                dateValue = (string?)null
            }
        });
        attrResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Viewer_should_not_access_attributes_manage_endpoints()
    {
        var token = await _factory.CreateAndLoginAsRoleAsync(_client, "Viewer");
        _client.SetBearer(token);

        var resp = await _client.GetAsync("/api/v1/attributes/definitions");
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Manager_should_access_people_write_endpoints()
    {
        var token = await _factory.CreateAndLoginAsRoleAsync(_client, "Manager");
        _client.SetBearer(token);

        var resp = await _client.PostAsJsonAsync("/api/v1/people", new
        {
            fullName = "Manager Can Create",
            identificationNumber = $"MGR-{Guid.NewGuid():N}",
            age = 31,
            gender = 1
        });

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Manager_should_not_access_attributes_manage_endpoints()
    {
        var token = await _factory.CreateAndLoginAsRoleAsync(_client, "Manager");
        _client.SetBearer(token);

        var resp = await _client.PostAsJsonAsync("/api/v1/attributes/definitions", new
        {
            key = $"mgr_attr_{Guid.NewGuid():N}",
            displayName = "Manager Forbidden",
            dataType = 2,
            isFilterable = true,
            validationRulesJson = (string?)null
        });

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Admin_should_access_attributes_manage_endpoints()
    {
        var token = await _factory.CreateAndLoginAsRoleAsync(_client, "Admin");
        _client.SetBearer(token);

        var resp = await _client.GetAsync("/api/v1/attributes/definitions");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<Guid> CreatePersonAsAdminAsync()
    {
        var adminToken = await _factory.CreateAndLoginAsRoleAsync(_client, "Admin");
        _client.SetBearer(adminToken);

        var createResp = await _client.PostAsJsonAsync("/api/v1/people", new
        {
            fullName = "Admin Seed Person",
            identificationNumber = $"AUTH-{Guid.NewGuid():N}",
            age = 29,
            gender = 1
        });
        createResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var created = await createResp.Content.ReadFromJsonAsync<PersonDto>();
        created.Should().NotBeNull();
        return created!.Id;
    }
}
