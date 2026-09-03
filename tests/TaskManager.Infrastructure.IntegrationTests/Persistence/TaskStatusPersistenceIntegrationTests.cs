using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using TaskManager.Infrastructure.IntegrationTests.Database;
using TaskManager.Infrastructure.Persistence.Repositories;
using Xunit;

namespace TaskManager.Infrastructure.IntegrationTests.Persistence;

[Collection(PostgreSqlCollectionDefinition.Name)]
public sealed class TaskStatusPersistenceIntegrationTests
{
    private readonly PostgreSqlFixture _fixture;

    public TaskStatusPersistenceIntegrationTests(
        PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task TaskItemRepositoryWhenStatusChangesPersistsStatusAndCompletionTime()
    {
        var cancellationToken =
            TestContext.Current.CancellationToken;

        var createdAtUtc =
            DateTimeOffset.UtcNow;

        var owner =
            User.Create(
                $"task-status-owner-{Guid.NewGuid():N}@example.com",
                "Task Status Owner",
                "integration-password-hash",
                createdAtUtc);

        var project =
            Project.Create(
                owner.Id,
                "Task Status Project",
                "Task status persistence integration test",
                createdAtUtc);

        var taskItem =
            TaskItem.Create(
                projectId: project.Id,
                createdByUserId: owner.Id,
                title: "Task status persistence",
                description: null,
                priority: TaskPriority.Medium,
                dueDateUtc: null,
                createdAtUtc: createdAtUtc);

        await using var dbContext =
            _fixture.CreateDbContext();

        var repository =
            new TaskItemRepository(dbContext);

        dbContext.Users.Add(
            owner);

        dbContext.Projects.Add(
            project);

        repository.Add(
            taskItem);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        dbContext.ChangeTracker.Clear();

        var trackedTask =
            await repository.GetByIdAsync(
                taskItem.Id,
                cancellationToken);

        Assert.NotNull(
            trackedTask);

        trackedTask.ChangeStatus(
            TaskItemStatus.Todo,
            createdAtUtc.AddMinutes(1));

        trackedTask.ChangeStatus(
            TaskItemStatus.InProgress,
            createdAtUtc.AddMinutes(2));

        trackedTask.ChangeStatus(
            TaskItemStatus.Review,
            createdAtUtc.AddMinutes(3));

        var completedAtUtc =
            createdAtUtc.AddMinutes(4);

        trackedTask.ChangeStatus(
            TaskItemStatus.Completed,
            completedAtUtc);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        dbContext.ChangeTracker.Clear();

        var persistedTask =
            await repository.GetByIdAsync(
                taskItem.Id,
                cancellationToken);

        Assert.NotNull(
            persistedTask);

        Assert.Equal(
            TaskItemStatus.Completed,
            persistedTask.Status);

        Assert.NotNull(
            persistedTask.UpdatedAtUtc);

        Assert.Equal(
            TruncateToMicroseconds(completedAtUtc),
            persistedTask.UpdatedAtUtc.Value);

        Assert.NotNull(
            persistedTask.CompletedAtUtc);

        Assert.Equal(
            TruncateToMicroseconds(completedAtUtc),
            persistedTask.CompletedAtUtc.Value);
    }

    private static DateTimeOffset TruncateToMicroseconds(
        DateTimeOffset value)
    {
        const long ticksPerMicrosecond = 10;

        var truncatedTicks =
            value.Ticks -
            value.Ticks % ticksPerMicrosecond;

        return new DateTimeOffset(
            truncatedTicks,
            value.Offset);
    }
}