using TaskManager.Domain.Entities;
using TaskManager.Infrastructure.IntegrationTests.Database;
using TaskManager.Infrastructure.Persistence.Repositories;
using Xunit;

namespace TaskManager.Infrastructure.IntegrationTests.Persistence;

[Collection(PostgreSqlCollectionDefinition.Name)]
public sealed class RepositoryIntegrationTests
{
    private readonly PostgreSqlFixture _fixture;

    public RepositoryIntegrationTests(
        PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ProjectRepositoryWhenProjectExistsReturnsPersistedProject()
    {
        var cancellationToken =
            TestContext.Current.CancellationToken;

        var createdAtUtc =
            DateTimeOffset.UtcNow;

        var user =
            User.Create(
                $"project-{Guid.NewGuid():N}@example.com",
                "Project Owner",
                "integration-password-hash",
                createdAtUtc);

        var project =
            Project.Create(
                user.Id,
                "Repository Project",
                "Repository integration test",
                createdAtUtc);

        await using var dbContext =
            _fixture.CreateDbContext();

        var repository =
            new ProjectRepository(dbContext);

        dbContext.Users.Add(user);

        repository.Add(project);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        dbContext.ChangeTracker.Clear();

        var persistedProject =
            await repository.GetByIdAsync(
                project.Id,
                cancellationToken);

        Assert.NotNull(persistedProject);

        Assert.Equal(
            project.Id,
            persistedProject.Id);

        Assert.Equal(
            user.Id,
            persistedProject.OwnerId);

        Assert.Equal(
            "Repository Project",
            persistedProject.Name);
    }

    [Fact]
    public async Task UserRepositoryWhenUserExistsFindsByNormalizedEmail()
    {
        var cancellationToken =
            TestContext.Current.CancellationToken;

        var createdAtUtc =
            DateTimeOffset.UtcNow;

        var user =
            User.Create(
                $"user-{Guid.NewGuid():N}@example.com",
                "Repository User",
                "integration-password-hash",
                createdAtUtc);

        await using var dbContext =
            _fixture.CreateDbContext();

        var repository =
            new UserRepository(dbContext);

        repository.Add(user);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        dbContext.ChangeTracker.Clear();

        var exists =
            await repository.ExistsByNormalizedEmailAsync(
                user.NormalizedEmail,
                cancellationToken);

        var persistedUser =
            await repository.GetByNormalizedEmailAsync(
                user.NormalizedEmail,
                cancellationToken);

        Assert.True(exists);

        Assert.NotNull(persistedUser);

        Assert.Equal(
            user.Id,
            persistedUser.Id);

        Assert.Equal(
            user.NormalizedEmail,
            persistedUser.NormalizedEmail);
    }
}