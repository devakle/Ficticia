using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Api.IntegrationTests.Fixtures;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _peopleDb;
    private readonly string _identityDb;

    public CustomWebApplicationFactory(string baseConn)
    {
        _peopleDb = baseConn.Replace("Database=master", "Database=FicticiaPeople_Test");
        _identityDb = baseConn.Replace("Database=master", "Database=FicticiaIdentity_Test");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((ctx, cfg) =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PeopleDb"] = _peopleDb,
                ["ConnectionStrings:IdentityDb"] = _identityDb,

                // evita pegarle a OpenAI en integration tests
                ["OpenAI:ApiKey"] = "disabled"
            });
        });

        builder.ConfigureServices(services =>
        {
            // CI usually does not provide Redis. Use in-memory distributed cache for deterministic tests.
            services.RemoveAll<IDistributedCache>();
            services.AddDistributedMemoryCache();
        });
    }
}
