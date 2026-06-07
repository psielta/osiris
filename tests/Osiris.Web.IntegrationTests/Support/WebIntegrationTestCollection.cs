namespace Osiris.Web.IntegrationTests.Support;

[CollectionDefinition(Name)]
public sealed class WebIntegrationTestCollection : ICollectionFixture<OsirisWebApplicationFactory>
{
    public const string Name = "Web integration tests";
}
