using Microsoft.EntityFrameworkCore;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using TaskManager.Infrastructure.IntegrationTests.Database;
using TaskManager.Infrastructure.Persistence.Repositories;
using Xunit;

namespace TaskManager.Infrastructure.IntegrationTests.Persistence;

[Collection(PostgreSqlCollectionDefinition.Name)]
public sealed class OptimisticConcurrencyIntegrationTests
{
    private readonly PostgreSqlFixture _fixture;

    public OptimisticConcurrencyIntegrationTests(
        PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ConcurrentProjectUpdatesRejectSecondWriter()
    {
        var token = TestContext.Current.CancellationToken;
        var now = DateTimeOffset.UtcNow;

        var owner = User.Create(
            $"project-concurrency-{Guid.NewGuid():N}@example.com",
            "Concurrency Owner",
            "integration-password-hash",
            now);

        var project = Project.Create(
            owner.Id,
            "Original Project",
            null,
            now);

        await using (var setup = _fixture.CreateDbContext())
        {
            setup.Users.Add(owner);
            setup.Projects.Add(project);
            await setup.SaveChangesAsync(token);
        }

        await using var firstDb = _fixture.CreateDbContext();
        await using var secondDb = _fixture.CreateDbContext();

        var firstRepository = new ProjectRepository(firstDb);
        var secondRepository = new ProjectRepository(secondDb);

        var first =
            await firstRepository.GetByIdForUpdateAsync(
                project.Id,
                token);

        var second =
            await secondRepository.GetByIdForUpdateAsync(
                project.Id,
                token);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(1, first.Version);
        Assert.Equal(1, second.Version);

        first.Rename(
            "First Writer",
            now.AddMinutes(1));

        second.Rename(
            "Second Writer",
            now.AddMinutes(2));

        await firstDb.SaveChangesAsync(token);

        Assert.Equal(2, first.Version);

        var exception =
            await Assert.ThrowsAsync<ApplicationConflictException>(
                () => secondDb.SaveChangesAsync(token));

        Assert.IsType<DbUpdateConcurrencyException>(
            exception.InnerException);
    }

    [Fact]
    public async Task ConcurrentTaskUpdatesRejectSecondWriter()
    {
        var token = TestContext.Current.CancellationToken;
        var now = DateTimeOffset.UtcNow;

        var owner = User.Create(
            $"task-concurrency-{Guid.NewGuid():N}@example.com",
            "Concurrency Owner",
            "integration-password-hash",
            now);

        var project = Project.Create(
            owner.Id,
            "Concurrency Project",
            null,
            now);

        var task = TaskItem.Create(
            project.Id,
            owner.Id,
            "Original Task",
            null,
            TaskPriority.Medium,
            null,
            now);

        await using (var setup = _fixture.CreateDbContext())
        {
            setup.Users.Add(owner);
            setup.Projects.Add(project);
            setup.TaskItems.Add(task);
            await setup.SaveChangesAsync(token);
        }

        await using var firstDb = _fixture.CreateDbContext();
        await using var secondDb = _fixture.CreateDbContext();

        var firstRepository = new TaskItemRepository(firstDb);
        var secondRepository = new TaskItemRepository(secondDb);

        var first =
            await firstRepository.GetByIdAsync(
                task.Id,
                token);

        var second =
            await secondRepository.GetByIdAsync(
                task.Id,
                token);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(1, first.Version);
        Assert.Equal(1, second.Version);

        first.Rename(
            "First Writer",
            now.AddMinutes(1));

        second.Rename(
            "Second Writer",
            now.AddMinutes(2));

        await firstDb.SaveChangesAsync(token);

        Assert.Equal(2, first.Version);

        var exception =
            await Assert.ThrowsAsync<ApplicationConflictException>(
                () => secondDb.SaveChangesAsync(token));

        Assert.IsType<DbUpdateConcurrencyException>(
            exception.InnerException);
    }
}
