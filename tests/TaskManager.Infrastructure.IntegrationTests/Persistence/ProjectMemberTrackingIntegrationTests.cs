using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using TaskManager.Infrastructure.IntegrationTests.Database;
using TaskManager.Infrastructure.Persistence.Repositories;
using Xunit;

namespace TaskManager.Infrastructure.IntegrationTests.Persistence;

[Collection(PostgreSqlCollectionDefinition.Name)]
public sealed class ProjectMemberTrackingIntegrationTests
{
    private readonly PostgreSqlFixture _fixture;

    public ProjectMemberTrackingIntegrationTests(
        PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ReadOnlyLookupDoesNotTrackMemberWhileUpdateLookupDoes()
    {
        var cancellationToken =
            TestContext.Current.CancellationToken;

        var now =
            DateTimeOffset.UtcNow;

        var owner =
            User.Create(
                $"member-tracking-owner-{Guid.NewGuid():N}@example.com",
                "Tracking Owner",
                "integration-password-hash",
                now);

        var member =
            User.Create(
                $"member-tracking-user-{Guid.NewGuid():N}@example.com",
                "Tracking Member",
                "integration-password-hash",
                now);

        var project =
            Project.Create(
                owner.Id,
                "Member Tracking Project",
                null,
                now);

        var membership =
            ProjectMember.Create(
                project.Id,
                member.Id,
                ProjectMemberRole.Member,
                now.AddMinutes(1));

        await using var dbContext =
            _fixture.CreateDbContext();

        dbContext.Users.AddRange(
            owner,
            member);

        dbContext.Projects.Add(
            project);

        dbContext.ProjectMembers.Add(
            membership);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        dbContext.ChangeTracker.Clear();

        var repository =
            new ProjectMemberRepository(
                dbContext);

        var readOnlyMembership =
            await repository.GetByProjectAndUserAsync(
                project.Id,
                member.Id,
                cancellationToken);

        Assert.NotNull(
            readOnlyMembership);

        Assert.Empty(
            dbContext.ChangeTracker
                .Entries<ProjectMember>());

        var trackedMembership =
            await repository.GetByProjectAndUserForUpdateAsync(
                project.Id,
                member.Id,
                cancellationToken);

        Assert.NotNull(
            trackedMembership);

        Assert.Single(
            dbContext.ChangeTracker
                .Entries<ProjectMember>());
    }
}
