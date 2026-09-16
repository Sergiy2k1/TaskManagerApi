using Microsoft.EntityFrameworkCore;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using TaskManager.Infrastructure.IntegrationTests.Database;
using TaskManager.Infrastructure.Persistence;
using Xunit;

namespace TaskManager.Infrastructure.IntegrationTests.Persistence;

[Collection(PostgreSqlCollectionDefinition.Name)]
public sealed class PersistenceMappingTests
{
    private readonly PostgreSqlFixture _fixture;

    public PersistenceMappingTests(
        PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task MigrationsWhenDatabaseStartsLeaveNoPendingMigrations()
    {
        await using var dbContext =
            _fixture.CreateDbContext();

        var pendingMigrations =
            await dbContext.Database
                .GetPendingMigrationsAsync(
                    TestContext.Current.CancellationToken);

        Assert.Empty(pendingMigrations);
    }

    [Fact]
    public async Task QueryPathIndexesAreApplied()
    {
        var cancellationToken =
            TestContext.Current.CancellationToken;

        await using var dbContext =
            _fixture.CreateDbContext();

        Assert.True(
            await IndexExistsAsync(
                dbContext,
                "ix_task_items_project_id_created_at_utc_id",
                cancellationToken));

        Assert.True(
            await IndexExistsAsync(
                dbContext,
                "ix_task_comments_task_item_id_created_at_utc_id",
                cancellationToken));

        Assert.True(
            await IndexExistsAsync(
                dbContext,
                "ix_project_members_active_project_joined_user",
                cancellationToken));
    }

    [Fact]
    public async Task SaveChangesWithCompleteGraphPersistsAllMappedEntities()
    {
        var cancellationToken =
            TestContext.Current.CancellationToken;

        var createdAtUtc =
            DateTimeOffset.UtcNow;

        var user =
            User.Create(
                $"integration-{Guid.NewGuid():N}@example.com",
                "Integration User",
                "integration-password-hash",
                createdAtUtc);

        var project =
            Project.Create(
                user.Id,
                "Integration Project",
                "Persistence integration test",
                createdAtUtc);

        var member =
            ProjectMember.Create(
                project.Id,
                user.Id,
                ProjectMemberRole.Manager,
                createdAtUtc);

        var task =
            TaskItem.Create(
                project.Id,
                user.Id,
                "Integration Task",
                "Verify persistence mappings",
                TaskPriority.High,
                createdAtUtc.AddDays(7),
                createdAtUtc);

        task.Assign(
            user.Id,
            createdAtUtc.AddMinutes(1));

        var comment =
            TaskComment.Create(
                task.Id,
                user.Id,
                "Integration comment",
                createdAtUtc.AddMinutes(2));

        await using var dbContext =
            _fixture.CreateDbContext();

        dbContext.Users.Add(user);
        dbContext.Projects.Add(project);
        dbContext.ProjectMembers.Add(member);
        dbContext.TaskItems.Add(task);
        dbContext.TaskComments.Add(comment);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        dbContext.ChangeTracker.Clear();

        var persistedUser =
            await dbContext.Users
                .AsNoTracking()
                .SingleAsync(
                    item => item.Id == user.Id,
                    cancellationToken);

        var persistedProject =
            await dbContext.Projects
                .AsNoTracking()
                .SingleAsync(
                    item => item.Id == project.Id,
                    cancellationToken);

        var persistedMember =
            await dbContext.ProjectMembers
                .AsNoTracking()
                .SingleAsync(
                    item => item.Id == member.Id,
                    cancellationToken);

        var persistedTask =
            await dbContext.TaskItems
                .AsNoTracking()
                .SingleAsync(
                    item => item.Id == task.Id,
                    cancellationToken);

        var persistedComment =
            await dbContext.TaskComments
                .AsNoTracking()
                .SingleAsync(
                    item => item.Id == comment.Id,
                    cancellationToken);

        Assert.Equal(
            user.NormalizedEmail,
            persistedUser.NormalizedEmail);

        Assert.Equal(
            project.OwnerId,
            persistedProject.OwnerId);

        Assert.Equal(
            ProjectMemberRole.Manager,
            persistedMember.Role);

        Assert.True(
            persistedMember.IsActive);

        Assert.Equal(
            TaskPriority.High,
            persistedTask.Priority);

        Assert.Equal(
            TaskItemStatus.Backlog,
            persistedTask.Status);

        Assert.Equal(
            user.Id,
            persistedTask.AssigneeId);

        Assert.Equal(
            task.Id,
            persistedComment.TaskItemId);

        Assert.Equal(
            user.Id,
            persistedComment.AuthorUserId);

        Assert.False(
            persistedComment.IsDeleted);
    }

    private static async Task<bool> IndexExistsAsync(
        AppDbContext dbContext,
        string indexName,
        CancellationToken cancellationToken)
    {
        await dbContext.Database.OpenConnectionAsync(
            cancellationToken);

        await using var command =
            dbContext.Database
                .GetDbConnection()
                .CreateCommand();

        command.CommandText =
            "SELECT to_regclass(@index_name) IS NOT NULL;";

        var parameter =
            command.CreateParameter();

        parameter.ParameterName =
            "index_name";

        parameter.Value =
            $"public.{indexName}";

        command.Parameters.Add(
            parameter);

        var result =
            await command.ExecuteScalarAsync(
                cancellationToken);

        return result is true;
    }

    [Fact]
    public async Task SaveChangesWithDuplicateProjectMemberThrowsConflictException()
    {
        var cancellationToken =
            TestContext.Current.CancellationToken;

        var createdAtUtc =
            DateTimeOffset.UtcNow;

        var user =
            User.Create(
                $"member-{Guid.NewGuid():N}@example.com",
                "Project Member",
                "integration-password-hash",
                createdAtUtc);

        var project =
            Project.Create(
                user.Id,
                "Unique Member Project",
                null,
                createdAtUtc);

        var firstMember =
            ProjectMember.Create(
                project.Id,
                user.Id,
                ProjectMemberRole.Manager,
                createdAtUtc);

        var duplicateMember =
            ProjectMember.Create(
                project.Id,
                user.Id,
                ProjectMemberRole.Member,
                createdAtUtc);

        await using var dbContext =
            _fixture.CreateDbContext();

        dbContext.Users.Add(user);
        dbContext.Projects.Add(project);
        dbContext.ProjectMembers.Add(firstMember);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        dbContext.ProjectMembers.Add(
            duplicateMember);

        var exception =
            await Assert.ThrowsAsync<ApplicationConflictException>(
                () => dbContext.SaveChangesAsync(
                    cancellationToken));

        Assert.Equal(
            "User is already an active project member.",
            exception.Message);

        Assert.IsType<DbUpdateException>(
            exception.InnerException);
    }
}