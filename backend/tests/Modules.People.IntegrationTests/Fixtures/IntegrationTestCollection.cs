using Xunit;

namespace Api.IntegrationTests.Fixtures;

[CollectionDefinition("integration")]
public sealed class IntegrationTestCollection : ICollectionFixture<MsSqlContainerFixture> { }
