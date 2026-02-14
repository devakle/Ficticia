using System.Net;
using System.Net.Http.Json;
using Api.IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Modules.AI.Application.Abstractions;
using Modules.AI.Application.Abstractions.Models;
using Modules.AI.Contracts.Dtos;
using Modules.People.Contracts.Dtos;
using Xunit;

[Collection("integration")]
public sealed class AiEndpointsTests
{
    private readonly MsSqlContainerFixture _sql;

    public AiEndpointsTests(MsSqlContainerFixture sql)
    {
        _sql = sql;
    }

    [Fact]
    public async Task Ai_endpoints_should_require_authentication()
    {
        using var factory = new AiTestWebApplicationFactory(
            _sql.ConnectionString,
            new FakeConditionNormalizer((_, _) => Task.FromResult(SuccessCondition())),
            new FakeRiskScorer((_, _) => Task.FromResult(new RiskScore(20, RiskBand.Low, new[] { "ok" }))));

        using var client = factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/v1/ai/conditions/normalize", new { text = "diabetes" });
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task Normalize_condition_should_return_ok_when_provider_succeeds()
    {
        using var factory = new AiTestWebApplicationFactory(
            _sql.ConnectionString,
            new FakeConditionNormalizer((_, _) => Task.FromResult(SuccessCondition())),
            new FakeRiskScorer((_, _) => Task.FromResult(new RiskScore(20, RiskBand.Low, new[] { "ok" }))));

        using var client = factory.CreateClient();
        var token = await client.LoginAsAdminAsync();
        client.SetBearer(token);

        var resp = await client.PostAsJsonAsync("/api/v1/ai/conditions/normalize", new { text = "Diabetes" });
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        using var body = await resp.ReadJsonAsync();
        Assert.Equal("diabetes", body.RootElement.GetProperty("code").GetString());
        Assert.Equal("fake", body.RootElement.GetProperty("source").GetString());
    }

    [Fact]
    public async Task Normalize_condition_should_return_bad_request_when_provider_fails()
    {
        using var factory = new AiTestWebApplicationFactory(
            _sql.ConnectionString,
            new FakeConditionNormalizer((_, _) => throw new InvalidOperationException("provider down")),
            new FakeRiskScorer((_, _) => Task.FromResult(new RiskScore(20, RiskBand.Low, new[] { "ok" }))));

        using var client = factory.CreateClient();
        var token = await client.LoginAsAdminAsync();
        client.SetBearer(token);

        var resp = await client.PostAsJsonAsync("/api/v1/ai/conditions/normalize", new { text = "Diabetes" });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);

        using var body = await resp.ReadJsonAsync();
        Assert.Equal("ai.provider_failed", body.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Risk_score_should_return_ok_when_provider_succeeds()
    {
        using var factory = new AiTestWebApplicationFactory(
            _sql.ConnectionString,
            new FakeConditionNormalizer((_, _) => Task.FromResult(SuccessCondition())),
            new FakeRiskScorer((personId, _) => Task.FromResult(new RiskScore(65, RiskBand.Medium, new[] { personId.ToString() }))));

        using var client = factory.CreateClient();
        var token = await client.LoginAsAdminAsync();
        client.SetBearer(token);

        var personId = await client.CreatePersonAsync();
        var resp = await client.PostAsync($"/api/v1/ai/people/{personId}/risk-score", content: null);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        using var body = await resp.ReadJsonAsync();
        Assert.Equal(65, body.RootElement.GetProperty("score").GetInt32());
        Assert.Equal((int)RiskBand.Medium, body.RootElement.GetProperty("band").GetInt32());
    }

    [Fact]
    public async Task Risk_score_should_return_bad_request_when_person_does_not_exist()
    {
        using var factory = new AiTestWebApplicationFactory(
            _sql.ConnectionString,
            new FakeConditionNormalizer((_, _) => Task.FromResult(SuccessCondition())),
            new FakeRiskScorer((_, _) => throw new KeyNotFoundException()));

        using var client = factory.CreateClient();
        var token = await client.LoginAsAdminAsync();
        client.SetBearer(token);

        var resp = await client.PostAsync($"/api/v1/ai/people/{Guid.NewGuid()}/risk-score", content: null);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);

        using var body = await resp.ReadJsonAsync();
        Assert.Equal("ai.person_not_found", body.RootElement.GetProperty("code").GetString());
    }

    private static NormalizedCondition SuccessCondition()
        => new(
            Code: "diabetes",
            Label: "Diabetes",
            Confidence: 0.95,
            MatchedTerms: new[] { "diabetes" },
            SuggestedAttributes: new[] { new UpsertAttributeValueDto("condition_code", null, "diabetes", null, null) },
            Source: "fake");

    private sealed class FakeConditionNormalizer : IConditionNormalizer
    {
        private readonly Func<string, CancellationToken, Task<NormalizedCondition>> _impl;

        public FakeConditionNormalizer(Func<string, CancellationToken, Task<NormalizedCondition>> impl)
        {
            _impl = impl;
        }

        public Task<NormalizedCondition> NormalizeAsync(string text, CancellationToken ct) => _impl(text, ct);
    }

    private sealed class FakeRiskScorer : IRiskScorer
    {
        private readonly Func<Guid, CancellationToken, Task<RiskScore>> _impl;

        public FakeRiskScorer(Func<Guid, CancellationToken, Task<RiskScore>> impl)
        {
            _impl = impl;
        }

        public Task<RiskScore> ScorePersonAsync(Guid personId, CancellationToken ct) => _impl(personId, ct);
    }

    private sealed class AiTestWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _peopleDb;
        private readonly string _identityDb;
        private readonly IConditionNormalizer _normalizer;
        private readonly IRiskScorer _riskScorer;

        public AiTestWebApplicationFactory(
            string baseConn,
            IConditionNormalizer normalizer,
            IRiskScorer riskScorer)
        {
            _peopleDb = baseConn.Replace("Database=master", "Database=FicticiaPeople_Test");
            _identityDb = baseConn.Replace("Database=master", "Database=FicticiaIdentity_Test");
            _normalizer = normalizer;
            _riskScorer = riskScorer;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, cfg) =>
            {
                cfg.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:PeopleDb"] = _peopleDb,
                    ["ConnectionStrings:IdentityDb"] = _identityDb,
                    ["OpenAI:ApiKey"] = "disabled"
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IConditionNormalizer>();
                services.RemoveAll<IRiskScorer>();
                services.AddScoped(_ => _normalizer);
                services.AddScoped(_ => _riskScorer);
            });
        }
    }
}
