using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Api.IntegrationTests.Fixtures;
using FluentAssertions;
using Xunit;

[Collection("integration")]
public sealed class PeopleEndpointsTests
{
    private readonly HttpClient _client;

    public PeopleEndpointsTests(MsSqlContainerFixture sql)
    {
        var factory = new CustomWebApplicationFactory(sql.ConnectionString);
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task People_endpoints_should_require_authentication()
    {
        var resp = await _client.GetAsync("/api/v1/people");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_and_get_by_id_should_work()
    {
        var token = await _client.LoginAsAdminAsync();
        _client.SetBearer(token);

        var personId = await _client.CreatePersonAsync();

        var get = await _client.GetAsync($"/api/v1/people/{personId}");
        get.StatusCode.Should().Be(HttpStatusCode.OK);

        using var body = await get.ReadJsonAsync();
        body.RootElement.GetProperty("id").GetGuid().Should().Be(personId);
    }

    [Fact]
    public async Task Create_should_fail_with_validation_error()
    {
        var token = await _client.LoginAsAdminAsync();
        _client.SetBearer(token);

        var create = await _client.PostAsJsonAsync("/api/v1/people", new
        {
            fullName = "",
            identificationNumber = "",
            age = 999,
            gender = 99
        });

        create.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var body = await create.ReadJsonAsync();
        body.RootElement.GetProperty("title").GetString().Should().Be("Validation error");
    }

    [Fact]
    public async Task Get_by_id_should_return_not_found_for_missing_person()
    {
        var token = await _client.LoginAsAdminAsync();
        _client.SetBearer(token);

        var get = await _client.GetAsync($"/api/v1/people/{Guid.NewGuid()}");
        get.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_should_persist_changes()
    {
        var token = await _client.LoginAsAdminAsync();
        _client.SetBearer(token);

        var personId = await _client.CreatePersonAsync(fullName: "Original Name");

        var update = await _client.PutAsJsonAsync($"/api/v1/people/{personId}", new
        {
            id = personId,
            fullName = "Updated Name",
            identificationNumber = $"UPD-{Guid.NewGuid():N}",
            age = 42,
            gender = 2
        });

        update.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var get = await _client.GetAsync($"/api/v1/people/{personId}");
        get.StatusCode.Should().Be(HttpStatusCode.OK);

        using var body = await get.ReadJsonAsync();
        body.RootElement.GetProperty("fullName").GetString().Should().Be("Updated Name");
        body.RootElement.GetProperty("age").GetInt32().Should().Be(42);
        body.RootElement.GetProperty("gender").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task Create_should_return_conflict_for_duplicate_identification()
    {
        var token = await _client.LoginAsAdminAsync();
        _client.SetBearer(token);

        var identification = $"DUP-{Guid.NewGuid():N}";
        await _client.CreatePersonAsync(identificationNumber: identification);

        var duplicate = await _client.PostAsJsonAsync("/api/v1/people", new
        {
            fullName = "Duplicate Person",
            identificationNumber = identification,
            age = 30,
            gender = 1
        });

        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);

        using var body = await duplicate.ReadJsonAsync();
        body.RootElement.GetProperty("code").GetString().Should().Be("people.duplicate_identification");
    }

    [Fact]
    public async Task Update_should_fail_when_route_and_body_ids_mismatch()
    {
        var token = await _client.LoginAsAdminAsync();
        _client.SetBearer(token);

        var id = Guid.NewGuid();
        var update = await _client.PutAsJsonAsync($"/api/v1/people/{id}", new
        {
            id = Guid.NewGuid(),
            fullName = "Mismatch",
            identificationNumber = $"MM-{Guid.NewGuid():N}",
            age = 30,
            gender = 1
        });

        update.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_should_return_not_found_for_missing_person()
    {
        var token = await _client.LoginAsAdminAsync();
        _client.SetBearer(token);

        var id = Guid.NewGuid();
        var update = await _client.PutAsJsonAsync($"/api/v1/people/{id}", new
        {
            id,
            fullName = "Missing",
            identificationNumber = $"MISS-{Guid.NewGuid():N}",
            age = 30,
            gender = 1
        });

        update.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_should_return_conflict_for_duplicate_identification()
    {
        var token = await _client.LoginAsAdminAsync();
        _client.SetBearer(token);

        var firstId = await _client.CreatePersonAsync(identificationNumber: $"A-{Guid.NewGuid():N}");
        var secondIdentification = $"B-{Guid.NewGuid():N}";
        var secondId = await _client.CreatePersonAsync(
            fullName: "Second Person",
            identificationNumber: secondIdentification);

        var update = await _client.PutAsJsonAsync($"/api/v1/people/{firstId}", new
        {
            id = firstId,
            fullName = "First Person Updated",
            identificationNumber = secondIdentification,
            age = 35,
            gender = 1
        });

        update.StatusCode.Should().Be(HttpStatusCode.Conflict);

        using var updateBody = await update.ReadJsonAsync();
        updateBody.RootElement.GetProperty("code").GetString().Should().Be("people.duplicate_identification");

        var getSecond = await _client.GetAsync($"/api/v1/people/{secondId}");
        getSecond.StatusCode.Should().Be(HttpStatusCode.OK);

        using var getSecondBody = await getSecond.ReadJsonAsync();
        getSecondBody.RootElement.GetProperty("identificationNumber").GetString().Should().Be(secondIdentification);
    }

    [Fact]
    public async Task Set_status_should_toggle_person_active_state()
    {
        var token = await _client.LoginAsAdminAsync();
        _client.SetBearer(token);

        var personId = await _client.CreatePersonAsync();

        var patch = await _client.PatchAsJsonAsync($"/api/v1/people/{personId}/status", new
        {
            id = personId,
            isActive = false
        });

        patch.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var get = await _client.GetAsync($"/api/v1/people/{personId}");
        get.StatusCode.Should().Be(HttpStatusCode.OK);

        using var body = await get.ReadJsonAsync();
        body.RootElement.GetProperty("isActive").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task Set_status_should_fail_when_route_and_body_ids_mismatch()
    {
        var token = await _client.LoginAsAdminAsync();
        _client.SetBearer(token);

        var patch = await _client.PatchAsJsonAsync($"/api/v1/people/{Guid.NewGuid()}/status", new
        {
            id = Guid.NewGuid(),
            isActive = false
        });

        patch.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Set_status_should_return_not_found_for_missing_person()
    {
        var token = await _client.LoginAsAdminAsync();
        _client.SetBearer(token);

        var id = Guid.NewGuid();
        var patch = await _client.PatchAsJsonAsync($"/api/v1/people/{id}/status", new
        {
            id,
            isActive = false
        });

        patch.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Search_should_filter_by_identification_and_active_state()
    {
        var token = await _client.LoginAsAdminAsync();
        _client.SetBearer(token);

        var identification = $"SEARCH-{Guid.NewGuid():N}";
        var personId = await _client.CreatePersonAsync(
            fullName: $"Search Person {Guid.NewGuid():N}",
            identificationNumber: identification);

        var search = await _client.GetAsync($"/api/v1/people?identificationNumber={identification}&isActive=true");
        search.StatusCode.Should().Be(HttpStatusCode.OK);

        using var body = await search.ReadJsonAsync();
        var items = body.RootElement.GetProperty("items").EnumerateArray().ToList();
        items.Should().Contain(x => x.GetProperty("id").GetGuid() == personId);
    }

    [Fact]
    public async Task Search_should_support_dynamic_filter_attr_dot_format()
    {
        var token = await _client.LoginAsAdminAsync();
        _client.SetBearer(token);

        var personId = await _client.CreatePersonAsync();

        var upsert = await _client.PutAsJsonAsync($"/api/v1/people/{personId}/attributes", new[]
        {
            new
            {
                key = "condition_code",
                boolValue = (bool?)null,
                stringValue = "diabetes",
                numberValue = (decimal?)null,
                dateValue = (string?)null
            }
        });
        upsert.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var search = await _client.GetAsync("/api/v1/people?attr.condition_code=diabetes");
        search.StatusCode.Should().Be(HttpStatusCode.OK);

        using var body = await search.ReadJsonAsync();
        var items = body.RootElement.GetProperty("items").EnumerateArray().ToList();
        items.Should().Contain(x => x.GetProperty("id").GetGuid() == personId);
    }

    [Fact]
    public async Task Search_should_support_dynamic_filter_attr_bracket_format()
    {
        var token = await _client.LoginAsAdminAsync();
        _client.SetBearer(token);

        var personId = await _client.CreatePersonAsync();

        var upsert = await _client.PutAsJsonAsync($"/api/v1/people/{personId}/attributes", new[]
        {
            new
            {
                key = "condition_code",
                boolValue = (bool?)null,
                stringValue = "asma",
                numberValue = (decimal?)null,
                dateValue = (string?)null
            }
        });
        upsert.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var search = await _client.GetAsync("/api/v1/people?attr[condition_code]=asma");
        search.StatusCode.Should().Be(HttpStatusCode.OK);

        using var body = await search.ReadJsonAsync();
        var items = body.RootElement.GetProperty("items").EnumerateArray().ToList();
        items.Should().Contain(x => x.GetProperty("id").GetGuid() == personId);
    }

    [Fact]
    public async Task Search_should_fail_with_invalid_dynamic_filter_key()
    {
        var token = await _client.LoginAsAdminAsync();
        _client.SetBearer(token);

        var search = await _client.GetAsync("/api/v1/people?attr.unknown_filter=true");
        search.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var body = await search.ReadJsonAsync();
        body.RootElement.GetProperty("code").GetString().Should().Be("filters.invalid");
    }

    [Fact]
    public async Task Search_should_fail_with_invalid_dynamic_filter_type()
    {
        var token = await _client.LoginAsAdminAsync();
        _client.SetBearer(token);

        var search = await _client.GetAsync("/api/v1/people?attr.diabetic=not-a-bool");
        search.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var body = await search.ReadJsonAsync();
        body.RootElement.GetProperty("code").GetString().Should().Be("filters.invalid");
    }

    [Fact]
    public async Task Get_attributes_should_return_not_found_for_missing_person()
    {
        var token = await _client.LoginAsAdminAsync();
        _client.SetBearer(token);

        var get = await _client.GetAsync($"/api/v1/people/{Guid.NewGuid()}/attributes");
        get.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_attributes_should_return_values_for_existing_person()
    {
        var token = await _client.LoginAsAdminAsync();
        _client.SetBearer(token);

        var personId = await _client.CreatePersonAsync();

        var upsert = await _client.PutAsJsonAsync($"/api/v1/people/{personId}/attributes", new[]
        {
            new
            {
                key = "diabetic",
                boolValue = true,
                stringValue = (string?)null,
                numberValue = (decimal?)null,
                dateValue = (string?)null
            }
        });
        upsert.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var get = await _client.GetAsync($"/api/v1/people/{personId}/attributes");
        get.StatusCode.Should().Be(HttpStatusCode.OK);

        using var body = await get.ReadJsonAsync();
        var items = body.RootElement.EnumerateArray().ToList();
        items.Should().Contain(x => x.GetProperty("key").GetString() == "diabetic");
    }

    [Fact]
    public async Task Get_attributes_form_should_return_catalog_for_existing_person()
    {
        var token = await _client.LoginAsAdminAsync();
        _client.SetBearer(token);

        var personId = await _client.CreatePersonAsync();

        var get = await _client.GetAsync($"/api/v1/people/{personId}/attributes/form?onlyActive=true");
        get.StatusCode.Should().Be(HttpStatusCode.OK);

        using var body = await get.ReadJsonAsync();
        var items = body.RootElement.EnumerateArray().ToList();
        items.Count.Should().BeGreaterThanOrEqualTo(5);
        items.Should().Contain(x => x.GetProperty("key").GetString() == "condition_code");
    }

    [Fact]
    public async Task Get_attributes_form_should_return_not_found_for_missing_person()
    {
        var token = await _client.LoginAsAdminAsync();
        _client.SetBearer(token);

        var get = await _client.GetAsync($"/api/v1/people/{Guid.NewGuid()}/attributes/form");
        get.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
