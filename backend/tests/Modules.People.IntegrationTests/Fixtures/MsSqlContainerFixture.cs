using Testcontainers.MsSql;

namespace Api.IntegrationTests.Fixtures;

public sealed class MsSqlContainerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container;

    public string ConnectionString => _container.GetConnectionString() + ";TrustServerCertificate=True;";

    public MsSqlContainerFixture()
    {
        _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("Your_password123!")
            .Build();
    }

    public Task InitializeAsync() => _container.StartAsync();
    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
