using TaskManager.Domain.Entities;
using TaskManager.Infrastructure.IntegrationTests.Database;
using TaskManager.Infrastructure.Persistence.Repositories;
using Xunit;

namespace TaskManager.Infrastructure.IntegrationTests.Persistence;

[Collection(PostgreSqlCollectionDefinition.Name)]
public sealed class ProjectLifecycleRepositoryIntegrationTests
{
    private readonly PostgreSqlFixture _fixture;

    public ProjectLifecycleRepositoryIntegrationTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ArchiveAndRestoreArePersisted()
    {
        var token = TestContext.Current.CancellationToken;
        var createdAtUtc = TruncateToMicroseconds(DateTimeOffset.UtcNow);

        var owner = User.Create(
            $"project-lifecycle-{Guid.NewGuid():N}@example.com",
            "Project Lifecycle Owner",
            "integration-password-hash",
            createdAtUtc);

        var project = Project.Create(
            owner.Id,
            "Lifecycle Project",
            null,
            createdAtUtc);

        await using var dbContext = _fixture.CreateDbContext();
        var repository = new ProjectRepository(dbContext);

        dbContext.Users.Add(owner);
        repository.Add(project);
        await dbContext.SaveChangesAsync(token);
        dbContext.ChangeTracker.Clear();

        var tracked = await repository.GetByIdForUpdateAsync(project.Id, token);
        Assert.NotNull(tracked);

        var archivedAtUtc = createdAtUtc.AddMinutes(1);
        tracked.Archive(archivedAtUtc);
        await dbContext.SaveChangesAsync(token);
        dbContext.ChangeTracker.Clear();

        var archived = await repository.GetByIdAsync(project.Id, token);
        Assert.NotNull(archived);
        Assert.True(archived.IsArchived);
        Assert.Equal(archivedAtUtc, archived.ArchivedAtUtc);

        var trackedAgain = await repository.GetByIdForUpdateAsync(project.Id, token);
        Assert.NotNull(trackedAgain);

        var restoredAtUtc = createdAtUtc.AddMinutes(2);
        trackedAgain.Restore(restoredAtUtc);
        await dbContext.SaveChangesAsync(token);
        dbContext.ChangeTracker.Clear();

        var restored = await repository.GetByIdAsync(project.Id, token);
        Assert.NotNull(restored);
        Assert.False(restored.IsArchived);
        Assert.Null(restored.ArchivedAtUtc);
        Assert.Equal(restoredAtUtc, restored.UpdatedAtUtc);
    }

    private static DateTimeOffset TruncateToMicroseconds(DateTimeOffset value)
    {
        const long ticksPerMicrosecond =
            TimeSpan.TicksPerMillisecond / 1000;

        var utcTicks =
            value.UtcTicks - value.UtcTicks % ticksPerMicrosecond;

        return new DateTimeOffset(utcTicks, TimeSpan.Zero);
    }
}
