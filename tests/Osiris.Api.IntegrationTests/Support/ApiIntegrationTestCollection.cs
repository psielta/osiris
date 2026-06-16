namespace Osiris.Api.IntegrationTests.Support;

[CollectionDefinition(Name)]
public sealed class ApiIntegrationTestCollection : ICollectionFixture<OsirisApiApplicationFactory>
{
    public const string Name = "API integration tests";
}
