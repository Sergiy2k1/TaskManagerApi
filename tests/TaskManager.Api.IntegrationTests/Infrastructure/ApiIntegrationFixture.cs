using Microsoft.EntityFrameworkCore;
using TaskManager.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Xunit;

namespace TaskManager.Api.IntegrationTests.Infrastructure;

public sealed class ApiIntegrationFixture
    : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container =
        new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("task_manager_api_tests")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

    private TaskManagerApiFactory? _factory;

    public HttpClient CreateClient()
    {
        if (_factory is null)
        {
            throw new InvalidOperationException(
                "API integration fixture has not been initialized.");
        }

        return _factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing
                .WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
    }

    public async Task ResetDatabaseAsync(
        CancellationToken cancellationToken = default)
    {
        await using var dbContext =
            CreateDbContext();

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            TRUNCATE TABLE
                task_comments,
                task_items,
                project_members,
                projects,
                users
            RESTART IDENTITY CASCADE;
            """,
            cancellationToken);
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

        _factory =
            new TaskManagerApiFactory(
                _container.GetConnectionString());
    }

    public async ValueTask DisposeAsync()
    {
        _factory?.Dispose();

        await _container
            .DisposeAsync()
            .ConfigureAwait(false);
    }

    private AppDbContext CreateDbContext()
    {
        var options =
            new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(
                    _container.GetConnectionString())
                .Options;

        return new AppDbContext(options);
    }
}
