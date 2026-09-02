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

    [Fact]
    public async Task TaskItemRepositoryWhenTaskExistsReturnsPersistedTask()
    {
        var cancellationToken =
            TestContext.Current.CancellationToken;

        var createdAtUtc =
            DateTimeOffset.UtcNow;

        var dueDateUtc =
            createdAtUtc.AddDays(1);

        var owner =
            User.Create(
                $"task-owner-{Guid.NewGuid():N}@example.com",
                "Task Owner",
                "integration-password-hash",
                createdAtUtc);

        var project =
            Project.Create(
                owner.Id,
                "Task Repository Project",
                "Task repository integration test",
                createdAtUtc);

        var taskItem =
            TaskItem.Create(
                projectId: project.Id,
                createdByUserId: owner.Id,
                title: "Integration task",
                description: "Persist and load task item",
                priority: TaskPriority.High,
                dueDateUtc: dueDateUtc,
                createdAtUtc: createdAtUtc);

        await using var dbContext =
            _fixture.CreateDbContext();

        var repository =
            new TaskItemRepository(dbContext);

        dbContext.Users.Add(owner);
        dbContext.Projects.Add(project);

        repository.Add(taskItem);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        dbContext.ChangeTracker.Clear();

        var persistedTask =
            await repository.GetByIdAsync(
                taskItem.Id,
                cancellationToken);

        Assert.NotNull(persistedTask);

        Assert.Equal(
            taskItem.Id,
            persistedTask.Id);

        Assert.Equal(
            project.Id,
            persistedTask.ProjectId);

        Assert.Equal(
            owner.Id,
            persistedTask.CreatedByUserId);

        Assert.Null(
            persistedTask.AssigneeId);

        Assert.Equal(
            "Integration task",
            persistedTask.Title);

        Assert.Equal(
            "Persist and load task item",
            persistedTask.Description);

        Assert.Equal(
            TaskItemStatus.Backlog,
            persistedTask.Status);

        Assert.Equal(
            TaskPriority.High,
            persistedTask.Priority);

        Assert.NotNull(
            persistedTask.DueDateUtc);

        Assert.Equal(
            TruncateToMicroseconds(dueDateUtc),
            persistedTask.DueDateUtc.Value);

        Assert.Equal(
            TruncateToMicroseconds(createdAtUtc),
            persistedTask.CreatedAtUtc);

        Assert.Null(
            persistedTask.UpdatedAtUtc);

        Assert.Null(
            persistedTask.CompletedAtUtc);
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
    [Fact]
    public async Task TaskItemRepositoryGetByProjectReturnsOnlyProjectTasksInCreationOrder()
    {
        var cancellationToken =
            TestContext.Current.CancellationToken;

        var createdAtUtc =
            DateTimeOffset.UtcNow;

        var owner =
            User.Create(
                $"task-list-owner-{Guid.NewGuid():N}@example.com",
                "Task List Owner",
                "integration-password-hash",
                createdAtUtc);

        var project =
            Project.Create(
                owner.Id,
                "Task List Project",
                "Task list repository integration test",
                createdAtUtc);

        var anotherProject =
            Project.Create(
                owner.Id,
                "Another Project",
                null,
                createdAtUtc);

        var firstTask =
            TaskItem.Create(
                projectId: project.Id,
                createdByUserId: owner.Id,
                title: "First project task",
                description: null,
                priority: TaskPriority.Medium,
                dueDateUtc: null,
                createdAtUtc: createdAtUtc.AddMinutes(1));

        var secondTask =
            TaskItem.Create(
                projectId: project.Id,
                createdByUserId: owner.Id,
                title: "Second project task",
                description: null,
                priority: TaskPriority.High,
                dueDateUtc: null,
                createdAtUtc: createdAtUtc.AddMinutes(2));

        var anotherProjectTask =
            TaskItem.Create(
                projectId: anotherProject.Id,
                createdByUserId: owner.Id,
                title: "Another project task",
                description: null,
                priority: TaskPriority.Low,
                dueDateUtc: null,
                createdAtUtc: createdAtUtc.AddMinutes(3));

        await using var dbContext =
            _fixture.CreateDbContext();

        var repository =
            new TaskItemRepository(dbContext);

        dbContext.Users.Add(owner);

        dbContext.Projects.AddRange(
            project,
            anotherProject);

        repository.Add(firstTask);
        repository.Add(secondTask);
        repository.Add(anotherProjectTask);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        dbContext.ChangeTracker.Clear();

        var taskItems =
            await repository.GetByProjectAsync(
                project.Id,
                cancellationToken);

        Assert.Equal(
            2,
            taskItems.Count);

        Assert.Equal(
            firstTask.Id,
            taskItems[0].Id);

        Assert.Equal(
            secondTask.Id,
            taskItems[1].Id);

        Assert.All(
            taskItems,
            taskItem =>
                Assert.Equal(
                    project.Id,
                    taskItem.ProjectId));

        Assert.DoesNotContain(
            taskItems,
            taskItem =>
                taskItem.Id == anotherProjectTask.Id);

        Assert.Empty(
            dbContext.ChangeTracker.Entries<TaskItem>());
    }
}