using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
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

    [Fact]
    public async Task ProjectMemberRepositoryWhenMemberExistsReturnsPersistedMember()
    {
        var cancellationToken =
            TestContext.Current.CancellationToken;

        var createdAtUtc =
            DateTimeOffset.UtcNow;

        var owner =
            User.Create(
                $"owner-{Guid.NewGuid():N}@example.com",
                "Project Owner",
                "integration-password-hash",
                createdAtUtc);

        var memberUser =
            User.Create(
                $"member-{Guid.NewGuid():N}@example.com",
                "Project Member",
                "integration-password-hash",
                createdAtUtc);

        var project =
            Project.Create(
                owner.Id,
                "Membership Project",
                "Project member repository integration test",
                createdAtUtc);

        var projectMember =
            ProjectMember.Create(
                project.Id,
                memberUser.Id,
                ProjectMemberRole.Member,
                createdAtUtc);

        await using var dbContext =
            _fixture.CreateDbContext();

        var repository =
            new ProjectMemberRepository(dbContext);

        dbContext.Users.Add(owner);
        dbContext.Users.Add(memberUser);
        dbContext.Projects.Add(project);

        repository.Add(projectMember);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        dbContext.ChangeTracker.Clear();

        var persistedMember =
            await repository.GetByProjectAndUserAsync(
                project.Id,
                memberUser.Id,
                cancellationToken);

        Assert.NotNull(persistedMember);

        Assert.Equal(
            projectMember.Id,
            persistedMember.Id);

        Assert.Equal(
            project.Id,
            persistedMember.ProjectId);

        Assert.Equal(
            memberUser.Id,
            persistedMember.UserId);

        Assert.Equal(
            ProjectMemberRole.Member,
            persistedMember.Role);

        Assert.True(
            persistedMember.IsActive);
    }

    [Fact]
    public async Task ProjectMemberRepositoryGetActiveByProjectReturnsOnlyActiveMembers()
    {
        var cancellationToken =
            TestContext.Current.CancellationToken;

        var createdAtUtc =
            DateTimeOffset.UtcNow;

        var owner =
            User.Create(
                $"owner-list-{Guid.NewGuid():N}@example.com",
                "Project Owner",
                "integration-password-hash",
                createdAtUtc);

        var activeUser =
            User.Create(
                $"active-member-{Guid.NewGuid():N}@example.com",
                "Active Member",
                "integration-password-hash",
                createdAtUtc);

        var removedUser =
            User.Create(
                $"removed-member-{Guid.NewGuid():N}@example.com",
                "Removed Member",
                "integration-password-hash",
                createdAtUtc);

        var project =
            Project.Create(
                owner.Id,
                "Members List Project",
                "Active members integration test",
                createdAtUtc);

        var ownerMembership =
            ProjectMember.Create(
                project.Id,
                owner.Id,
                ProjectMemberRole.Manager,
                createdAtUtc);

        var activeMembership =
            ProjectMember.Create(
                project.Id,
                activeUser.Id,
                ProjectMemberRole.Member,
                createdAtUtc.AddMinutes(1));

        var removedMembership =
            ProjectMember.Create(
                project.Id,
                removedUser.Id,
                ProjectMemberRole.Member,
                createdAtUtc.AddMinutes(2));

        removedMembership.Remove(
            createdAtUtc.AddMinutes(3));

        await using var dbContext =
            _fixture.CreateDbContext();

        var repository =
            new ProjectMemberRepository(dbContext);

        dbContext.Users.AddRange(
            owner,
            activeUser,
            removedUser);

        dbContext.Projects.Add(project);

        repository.Add(ownerMembership);
        repository.Add(activeMembership);
        repository.Add(removedMembership);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        dbContext.ChangeTracker.Clear();

        var members =
            await repository.GetActiveByProjectAsync(
                project.Id,
                cancellationToken);

        Assert.Equal(
            2,
            members.Count);

        Assert.Equal(
            ownerMembership.Id,
            members[0].Id);

        Assert.Equal(
            activeMembership.Id,
            members[1].Id);

        Assert.DoesNotContain(
            members,
            member =>
                member.Id == removedMembership.Id);

        Assert.All(
            members,
            member =>
                Assert.True(member.IsActive));
    }
}