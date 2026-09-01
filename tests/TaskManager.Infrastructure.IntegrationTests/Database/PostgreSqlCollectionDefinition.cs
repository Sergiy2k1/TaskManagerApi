using Xunit;

namespace TaskManager.Infrastructure.IntegrationTests.Database;

[CollectionDefinition(Name)]
public sealed class PostgreSqlCollectionDefinition
    : ICollectionFixture<PostgreSqlFixture>
{
    public const string Name =
        "PostgreSQL integration tests";
}