using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

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
    }
}
