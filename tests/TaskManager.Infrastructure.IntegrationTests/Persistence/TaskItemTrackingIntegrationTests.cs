using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using TaskManager.Infrastructure.IntegrationTests.Database;
using TaskManager.Infrastructure.Persistence.Repositories;
using Xunit;

namespace TaskManager.Infrastructure.IntegrationTests.Persistence;

[Collection(PostgreSqlCollectionDefinition.Name)]
public sealed class TaskItemTrackingIntegrationTests
{
    private readonly PostgreSqlFixture _fixture;

    public TaskItemTrackingIntegrationTests(
        PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ReadOnlyLookupDoesNotTrackTaskWhileUpdateLookupDoes()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var now = DateTimeOffset.UtcNow;

        var owner = User.Create(
            $"tracking-owner-{Guid.NewGuid():N}@example.com",
            "Tracking Owner",
            "integration-password-hash",
            now);

        var project = Project.Create(
            owner.Id,
            "Tracking Project",
            null,
            now);

        var task = TaskItem.Create(
            project.Id,
            owner.Id,
            "Tracking task",
            null,
            TaskPriority.Medium,
            null,
            now.AddMinutes(1));

        await using var dbContext = _fixture.CreateDbContext();

        dbContext.Users.Add(owner);
        dbContext.Projects.Add(project);
        dbContext.TaskItems.Add(task);

        await dbContext.SaveChangesAsync(cancellationToken);
        dbContext.ChangeTracker.Clear();

        var repository = new TaskItemRepository(dbContext);

        var readOnlyTask = await repository.GetByIdReadOnlyAsync(
            task.Id,
            cancellationToken);

        Assert.NotNull(readOnlyTask);
        Assert.Empty(dbContext.ChangeTracker.Entries<TaskItem>());

        var trackedTask = await repository.GetByIdAsync(
            task.Id,
            cancellationToken);

        Assert.NotNull(trackedTask);
        Assert.Single(dbContext.ChangeTracker.Entries<TaskItem>());
    }
}
