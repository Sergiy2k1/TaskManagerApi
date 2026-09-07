using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using TaskManager.Infrastructure.IntegrationTests.Database;
using TaskManager.Infrastructure.Persistence.Repositories;
using Xunit;

namespace TaskManager.Infrastructure.IntegrationTests.Persistence;

[Collection(PostgreSqlCollectionDefinition.Name)]
public sealed class ProjectListingRepositoryIntegrationTests
{
    private readonly PostgreSqlFixture _fixture;

    public ProjectListingRepositoryIntegrationTests(
        PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetAccessibleByUserIdAsyncReturnsOwnedAndActiveMemberProjectsOnly()
    {
        var cancellationToken =
            TestContext.Current.CancellationToken;

        var createdAtUtc =
            DateTimeOffset.UtcNow;

        var currentUser =
            User.Create(
                $"current-{Guid.NewGuid():N}@example.com",
                "Current User",
                "integration-password-hash",
                createdAtUtc);

        var otherOwner =
            User.Create(
                $"owner-{Guid.NewGuid():N}@example.com",
                "Other Owner",
                "integration-password-hash",
                createdAtUtc);

        var ownedProject =
            Project.Create(
                currentUser.Id,
                "Owned Project",
                null,
                createdAtUtc);

        var activeMemberProject =
            Project.Create(
                otherOwner.Id,
                "Active Membership Project",
                null,
                createdAtUtc.AddMinutes(1));

        var removedMemberProject =
            Project.Create(
                otherOwner.Id,
                "Removed Membership Project",
                null,
                createdAtUtc.AddMinutes(2));

        var foreignProject =
            Project.Create(
                otherOwner.Id,
                "Foreign Project",
                null,
                createdAtUtc.AddMinutes(3));

        var activeMembership =
            ProjectMember.Create(
                activeMemberProject.Id,
                currentUser.Id,
                ProjectMemberRole.Member,
                createdAtUtc.AddMinutes(4));

        var removedMembership =
            ProjectMember.Create(
                removedMemberProject.Id,
                currentUser.Id,
                ProjectMemberRole.Member,
                createdAtUtc.AddMinutes(5));

        removedMembership.Remove(
            createdAtUtc.AddMinutes(6));

        await using var dbContext =
            _fixture.CreateDbContext();

        dbContext.Users.AddRange(
            currentUser,
            otherOwner);

        dbContext.Projects.AddRange(
            ownedProject,
            activeMemberProject,
            removedMemberProject,
            foreignProject);

        dbContext.ProjectMembers.AddRange(
            activeMembership,
            removedMembership);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        dbContext.ChangeTracker.Clear();

        var repository =
            new ProjectRepository(dbContext);

        var projects =
            await repository.GetAccessibleByUserIdAsync(
                currentUser.Id,
                cancellationToken);

        Assert.Equal(
            2,
            projects.Count);

        Assert.Contains(
            projects,
            project => project.Id == ownedProject.Id);

        Assert.Contains(
            projects,
            project => project.Id == activeMemberProject.Id);

        Assert.DoesNotContain(
            projects,
            project => project.Id == removedMemberProject.Id);

        Assert.DoesNotContain(
            projects,
            project => project.Id == foreignProject.Id);
    }
}
