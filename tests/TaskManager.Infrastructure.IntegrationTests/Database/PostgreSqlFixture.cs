using Microsoft.EntityFrameworkCore;
using TaskManager.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Xunit;

namespace TaskManager.Infrastructure.IntegrationTests.Database;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container =
        new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("task_manager_tests")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

    public string ConnectionString =>
        _container.GetConnectionString();

    public AppDbContext CreateDbContext()
    {
        var options =
            new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(ConnectionString)
                .Options;

        return new AppDbContext(options);
    }

    public async ValueTask InitializeAsync()
    {
        await _container
            .StartAsync()
            .ConfigureAwait(false);

        await using var dbContext =
            CreateDbContext();

        await dbContext.Database
            .MigrateAsync()
            .ConfigureAwait(false);
    }

    public ValueTask DisposeAsync()
    {
        return _container.DisposeAsync();
    }
}