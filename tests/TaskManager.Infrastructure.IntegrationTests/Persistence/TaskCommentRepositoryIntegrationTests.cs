using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using TaskManager.Infrastructure.IntegrationTests.Database;
using TaskManager.Infrastructure.Persistence.Repositories;
using Xunit;

namespace TaskManager.Infrastructure.IntegrationTests.Persistence;

[Collection(PostgreSqlCollectionDefinition.Name)]
public sealed class TaskCommentRepositoryIntegrationTests
{
    private readonly PostgreSqlFixture _fixture;

    public TaskCommentRepositoryIntegrationTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ActiveCommentsAreListedAndDeletedCommentsAreFilteredOut()
    {
        var token = TestContext.Current.CancellationToken;
        var now = TruncateToMicroseconds(DateTimeOffset.UtcNow);

        var user = User.Create(
            $"comment-{Guid.NewGuid():N}@example.com",
            "Comment User",
            "hash",
            now);

        var project = Project.Create(user.Id, "Comments", null, now);
        var task = TaskItem.Create(
            project.Id,
            user.Id,
            "Task",
            null,
            TaskPriority.Medium,
            null,
            now);

        var active = TaskComment.Create(task.Id, user.Id, "Active", now);
        var deleted = TaskComment.Create(task.Id, user.Id, "Deleted", now.AddSeconds(1));
        deleted.Delete(now.AddSeconds(2));

        await using var db = _fixture.CreateDbContext();
        db.Users.Add(user);
        db.Projects.Add(project);
        db.TaskItems.Add(task);
        db.TaskComments.AddRange(active, deleted);
        await db.SaveChangesAsync(token);
        db.ChangeTracker.Clear();

        var repository = new TaskCommentRepository(db);
        var comments = await repository.GetActiveByTaskAsync(task.Id, token);

        Assert.Single(comments);
        Assert.Equal(active.Id, comments[0].Id);
    }

    private static DateTimeOffset TruncateToMicroseconds(DateTimeOffset value)
    {
        const long ticksPerMicrosecond =
            TimeSpan.TicksPerMillisecond / 1000;

        var ticks = value.UtcTicks - value.UtcTicks % ticksPerMicrosecond;
        return new DateTimeOffset(ticks, TimeSpan.Zero);
    }
}
