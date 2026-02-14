using System.Net;
using System.Net.Http.Json;
using Api.IntegrationTests.Fixtures;
using FluentAssertions;
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
}
