using Xunit;

namespace TaskManager.Api.IntegrationTests.Infrastructure;

[CollectionDefinition(Name)]
public sealed class ApiIntegrationCollectionDefinition
    : ICollectionFixture<ApiIntegrationFixture>
{
    public const string Name =
        "API integration tests";
}
