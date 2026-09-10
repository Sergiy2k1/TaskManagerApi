using TaskManager.Domain.Entities;
using TaskManager.Infrastructure.IntegrationTests.Database;
using TaskManager.Infrastructure.Persistence.Repositories;
using Xunit;

namespace TaskManager.Infrastructure.IntegrationTests.Persistence;

[Collection(PostgreSqlCollectionDefinition.Name)]
public sealed class ProjectUpdateRepositoryIntegrationTests
{
    private readonly PostgreSqlFixture _fixture;

    public ProjectUpdateRepositoryIntegrationTests(
        PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetByIdForUpdateAsyncTracksProjectAndPersistsChanges()
    {
        var cancellationToken =
            TestContext.Current.CancellationToken;

        var createdAtUtc =
            TruncateToMicroseconds(DateTimeOffset.UtcNow);

        var owner =
            User.Create(
                $"project-update-{Guid.NewGuid():N}@example.com",
                "Project Update Owner",
                "integration-password-hash",
                createdAtUtc);

        var project =
            Project.Create(
                owner.Id,
                "Original Project",
                "Original description",
                createdAtUtc);

        await using var dbContext =
            _fixture.CreateDbContext();

        var repository =
            new ProjectRepository(dbContext);

        dbContext.Users.Add(owner);
        repository.Add(project);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        dbContext.ChangeTracker.Clear();

        var trackedProject =
            await repository.GetByIdForUpdateAsync(
                project.Id,
                cancellationToken);

        Assert.NotNull(trackedProject);

        var changedAtUtc =
            createdAtUtc.AddMinutes(1);

        trackedProject.Rename(
            "Updated Project",
            changedAtUtc);

        trackedProject.ChangeDescription(
            "Updated description",
            changedAtUtc);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        dbContext.ChangeTracker.Clear();

        var persistedProject =
            await repository.GetByIdAsync(
                project.Id,
                cancellationToken);

        Assert.NotNull(persistedProject);
        Assert.Equal(
            "Updated Project",
            persistedProject.Name);
        Assert.Equal(
            "Updated description",
            persistedProject.Description);
        Assert.Equal(
            changedAtUtc,
            persistedProject.UpdatedAtUtc);
    }

    private static DateTimeOffset TruncateToMicroseconds(
        DateTimeOffset value)
    {
        const long ticksPerMicrosecond =
            TimeSpan.TicksPerMillisecond / 1000;

        var utcTicks =
            value.UtcTicks -
            value.UtcTicks % ticksPerMicrosecond;

        return new DateTimeOffset(
            utcTicks,
            TimeSpan.Zero);
    }
}
