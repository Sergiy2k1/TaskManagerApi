using NSubstitute;
using TaskManager.Application.Abstractions.Authentication;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Common.Authorization;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Tasks.GetByProject;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using Xunit;

namespace TaskManager.Application.UnitTests.Tasks.GetByProject;

public sealed class GetProjectTasksHandlerTests
{
    [Fact]
    public async Task HandleAsyncWhenCurrentUserIsOwnerReturnsTasks()
    {
        var projectRepository =
            Substitute.For<IProjectRepository>();

        var projectMemberRepository =
            Substitute.For<IProjectMemberRepository>();

        var taskItemRepository =
            Substitute.For<ITaskItemRepository>();

        var currentUser =
            Substitute.For<ICurrentUser>();

        var now =
            CreateUtcTime();

        var ownerId =
            Guid.NewGuid();

        var project =
            Project.Create(
                ownerId,
                "Task Project",
                null,
                now);

        var firstTask =
            TaskItem.Create(
                projectId: project.Id,
                createdByUserId: ownerId,
                title: "First task",
                description: "First description",
                priority: TaskPriority.High,
                dueDateUtc: now.AddDays(2),
                createdAtUtc: now);

        var secondTask =
            TaskItem.Create(
                projectId: project.Id,
                createdByUserId: ownerId,
                title: "Second task",
                description: null,
                priority: TaskPriority.Low,
                dueDateUtc: null,
                createdAtUtc: now.AddMinutes(1));

        IReadOnlyList<TaskItem> taskItems =
        [
            firstTask,
            secondTask
        ];

        currentUser.UserId.Returns(
            ownerId);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        projectRepository
            .GetByIdAsync(
                project.Id,
                cancellationToken)
            .Returns(project);

        taskItemRepository
            .GetByProjectAsync(
                project.Id,
                cancellationToken)
            .Returns(taskItems);

        var handler =
            CreateHandler(
                projectRepository,
                projectMemberRepository,
                taskItemRepository,
                currentUser);

        var query =
            new GetProjectTasksQuery(
                project.Id);

        var result =
            await handler.HandleAsync(
                query,
                cancellationToken);

        Assert.Equal(
            2,
            result.Count);

        Assert.Equal(
            firstTask.Id,
            result[0].TaskItemId);

        Assert.Equal(
            project.Id,
            result[0].ProjectId);

        Assert.Equal(
            ownerId,
            result[0].CreatedByUserId);

        Assert.Null(
            result[0].AssigneeId);

        Assert.Equal(
            "First task",
            result[0].Title);

        Assert.Equal(
            "First description",
            result[0].Description);

        Assert.Equal(
            TaskItemStatus.Backlog,
            result[0].Status);

        Assert.Equal(
            TaskPriority.High,
            result[0].Priority);

        Assert.Equal(
            now.AddDays(2),
            result[0].DueDateUtc);

        Assert.Equal(
            now,
            result[0].CreatedAtUtc);

        Assert.Null(
            result[0].UpdatedAtUtc);

        Assert.Null(
            result[0].CompletedAtUtc);

        Assert.Equal(
            secondTask.Id,
            result[1].TaskItemId);

        Assert.Equal(
            "Second task",
            result[1].Title);

        Assert.Equal(
            TaskPriority.Low,
            result[1].Priority);

        await projectMemberRepository
            .DidNotReceive()
            .GetByProjectAndUserAsync(
                project.Id,
                ownerId,
                Arg.Any<CancellationToken>());

        await taskItemRepository
            .Received(1)
            .GetByProjectAsync(
                project.Id,
                cancellationToken);
    }

    [Fact]
    public async Task HandleAsyncWhenCurrentUserIsActiveMemberReturnsTasks()
    {
        var projectRepository =
            Substitute.For<IProjectRepository>();

        var projectMemberRepository =
            Substitute.For<IProjectMemberRepository>();

        var taskItemRepository =
            Substitute.For<ITaskItemRepository>();

        var currentUser =
            Substitute.For<ICurrentUser>();

        var now =
            CreateUtcTime();

        var ownerId =
            Guid.NewGuid();

        var memberId =
            Guid.NewGuid();

        var project =
            Project.Create(
                ownerId,
                "Member Project",
                null,
                now);

        var membership =
            ProjectMember.Create(
                project.Id,
                memberId,
                ProjectMemberRole.Member,
                now);

        var taskItem =
            TaskItem.Create(
                projectId: project.Id,
                createdByUserId: ownerId,
                title: "Visible task",
                description: null,
                priority: TaskPriority.Medium,
                dueDateUtc: null,
                createdAtUtc: now);

        IReadOnlyList<TaskItem> taskItems =
        [
            taskItem
        ];

        currentUser.UserId.Returns(
            memberId);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        projectRepository
            .GetByIdAsync(
                project.Id,
                cancellationToken)
            .Returns(project);

        projectMemberRepository
            .GetByProjectAndUserAsync(
                project.Id,
                memberId,
                cancellationToken)
            .Returns(membership);

        taskItemRepository
            .GetByProjectAsync(
                project.Id,
                cancellationToken)
            .Returns(taskItems);

        var handler =
            CreateHandler(
                projectRepository,
                projectMemberRepository,
                taskItemRepository,
                currentUser);

        var query =
            new GetProjectTasksQuery(
                project.Id);

        var result =
            await handler.HandleAsync(
                query,
                cancellationToken);

        Assert.Single(result);

        Assert.Equal(
            taskItem.Id,
            result[0].TaskItemId);

        Assert.Equal(
            "Visible task",
            result[0].Title);

        await projectMemberRepository
            .Received(1)
            .GetByProjectAndUserAsync(
                project.Id,
                memberId,
                cancellationToken);

        await taskItemRepository
            .Received(1)
            .GetByProjectAsync(
                project.Id,
                cancellationToken);
    }

    [Fact]
    public async Task HandleAsyncWhenProjectHasNoTasksReturnsEmptyList()
    {
        var projectRepository =
            Substitute.For<IProjectRepository>();

        var projectMemberRepository =
            Substitute.For<IProjectMemberRepository>();

        var taskItemRepository =
            Substitute.For<ITaskItemRepository>();

        var currentUser =
            Substitute.For<ICurrentUser>();

        var now =
            CreateUtcTime();

        var ownerId =
            Guid.NewGuid();

        var project =
            Project.Create(
                ownerId,
                "Empty Project",
                null,
                now);

        currentUser.UserId.Returns(
            ownerId);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        projectRepository
            .GetByIdAsync(
                project.Id,
                cancellationToken)
            .Returns(project);

        taskItemRepository
            .GetByProjectAsync(
                project.Id,
                cancellationToken)
            .Returns(Array.Empty<TaskItem>());

        var handler =
            CreateHandler(
                projectRepository,
                projectMemberRepository,
                taskItemRepository,
                currentUser);

        var query =
            new GetProjectTasksQuery(
                project.Id);

        var result =
            await handler.HandleAsync(
                query,
                cancellationToken);

        Assert.Empty(result);

        await taskItemRepository
            .Received(1)
            .GetByProjectAsync(
                project.Id,
                cancellationToken);
    }

    [Fact]
    public async Task HandleAsyncWhenCurrentUserWasRemovedThrowsNotFoundException()
    {
        var projectRepository =
            Substitute.For<IProjectRepository>();

        var projectMemberRepository =
            Substitute.For<IProjectMemberRepository>();

        var taskItemRepository =
            Substitute.For<ITaskItemRepository>();

        var currentUser =
            Substitute.For<ICurrentUser>();

        var now =
            CreateUtcTime();

        var ownerId =
            Guid.NewGuid();

        var memberId =
            Guid.NewGuid();

        var project =
            Project.Create(
                ownerId,
                "Removed Member Project",
                null,
                now);

        var membership =
            ProjectMember.Create(
                project.Id,
                memberId,
                ProjectMemberRole.Member,
                now);

        membership.Remove(
            now.AddMinutes(1));

        currentUser.UserId.Returns(
            memberId);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        projectRepository
            .GetByIdAsync(
                project.Id,
                cancellationToken)
            .Returns(project);

        projectMemberRepository
            .GetByProjectAndUserAsync(
                project.Id,
                memberId,
                cancellationToken)
            .Returns(membership);

        var handler =
            CreateHandler(
                projectRepository,
                projectMemberRepository,
                taskItemRepository,
                currentUser);

        var query =
            new GetProjectTasksQuery(
                project.Id);

        var exception =
            await Assert.ThrowsAsync<ApplicationNotFoundException>(
                () => handler.HandleAsync(
                    query,
                    cancellationToken));

        Assert.Equal(
            "Project was not found.",
            exception.Message);

        await taskItemRepository
            .DidNotReceive()
            .GetByProjectAsync(
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsyncWhenCurrentUserIsOutsiderThrowsNotFoundException()
    {
        var projectRepository =
            Substitute.For<IProjectRepository>();

        var projectMemberRepository =
            Substitute.For<IProjectMemberRepository>();

        var taskItemRepository =
            Substitute.For<ITaskItemRepository>();

        var currentUser =
            Substitute.For<ICurrentUser>();

        var now =
            CreateUtcTime();

        var ownerId =
            Guid.NewGuid();

        var outsiderId =
            Guid.NewGuid();

        var project =
            Project.Create(
                ownerId,
                "Private Project",
                null,
                now);

        currentUser.UserId.Returns(
            outsiderId);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        projectRepository
            .GetByIdAsync(
                project.Id,
                cancellationToken)
            .Returns(project);

        projectMemberRepository
            .GetByProjectAndUserAsync(
                project.Id,
                outsiderId,
                cancellationToken)
            .Returns((ProjectMember?)null);

        var handler =
            CreateHandler(
                projectRepository,
                projectMemberRepository,
                taskItemRepository,
                currentUser);

        var query =
            new GetProjectTasksQuery(
                project.Id);

        var exception =
            await Assert.ThrowsAsync<ApplicationNotFoundException>(
                () => handler.HandleAsync(
                    query,
                    cancellationToken));

        Assert.Equal(
            "Project was not found.",
            exception.Message);

        await taskItemRepository
            .DidNotReceive()
            .GetByProjectAsync(
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsyncWhenProjectDoesNotExistThrowsNotFoundException()
    {
        var projectRepository =
            Substitute.For<IProjectRepository>();

        var projectMemberRepository =
            Substitute.For<IProjectMemberRepository>();

        var taskItemRepository =
            Substitute.For<ITaskItemRepository>();

        var currentUser =
            Substitute.For<ICurrentUser>();

        var projectId =
            Guid.NewGuid();

        var cancellationToken =
            TestContext.Current.CancellationToken;

        projectRepository
            .GetByIdAsync(
                projectId,
                cancellationToken)
            .Returns((Project?)null);

        var handler =
            CreateHandler(
                projectRepository,
                projectMemberRepository,
                taskItemRepository,
                currentUser);

        var query =
            new GetProjectTasksQuery(
                projectId);

        var exception =
            await Assert.ThrowsAsync<ApplicationNotFoundException>(
                () => handler.HandleAsync(
                    query,
                    cancellationToken));

        Assert.Equal(
            "Project was not found.",
            exception.Message);

        await taskItemRepository
            .DidNotReceive()
            .GetByProjectAsync(
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsyncWhenProjectIsArchivedStillReturnsTasks()
    {
        var projectRepository =
            Substitute.For<IProjectRepository>();

        var projectMemberRepository =
            Substitute.For<IProjectMemberRepository>();

        var taskItemRepository =
            Substitute.For<ITaskItemRepository>();

        var currentUser =
            Substitute.For<ICurrentUser>();

        var now =
            CreateUtcTime();

        var ownerId =
            Guid.NewGuid();

        var project =
            Project.Create(
                ownerId,
                "Archived Project",
                null,
                now);

        var taskItem =
            TaskItem.Create(
                projectId: project.Id,
                createdByUserId: ownerId,
                title: "Archived project task",
                description: null,
                priority: TaskPriority.Medium,
                dueDateUtc: null,
                createdAtUtc: now);

        project.Archive(
            now.AddMinutes(1));

        IReadOnlyList<TaskItem> taskItems =
        [
            taskItem
        ];

        currentUser.UserId.Returns(
            ownerId);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        projectRepository
            .GetByIdAsync(
                project.Id,
                cancellationToken)
            .Returns(project);

        taskItemRepository
            .GetByProjectAsync(
                project.Id,
                cancellationToken)
            .Returns(taskItems);

        var handler =
            CreateHandler(
                projectRepository,
                projectMemberRepository,
                taskItemRepository,
                currentUser);

        var query =
            new GetProjectTasksQuery(
                project.Id);

        var result =
            await handler.HandleAsync(
                query,
                cancellationToken);

        Assert.Single(result);

        Assert.Equal(
            taskItem.Id,
            result[0].TaskItemId);
    }

    [Fact]
    public async Task HandleAsyncWhenProjectIdIsEmptyThrowsValidationException()
    {
        var projectRepository =
            Substitute.For<IProjectRepository>();

        var projectMemberRepository =
            Substitute.For<IProjectMemberRepository>();

        var taskItemRepository =
            Substitute.For<ITaskItemRepository>();

        var currentUser =
            Substitute.For<ICurrentUser>();

        var handler =
            CreateHandler(
                projectRepository,
                projectMemberRepository,
                taskItemRepository,
                currentUser);

        var query =
            new GetProjectTasksQuery(
                Guid.Empty);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        var exception =
            await Assert.ThrowsAsync<ApplicationValidationException>(
                () => handler.HandleAsync(
                    query,
                    cancellationToken));

        Assert.Equal(
            "Project identifier cannot be empty.",
            exception.Message);

        await projectRepository
            .DidNotReceive()
            .GetByIdAsync(
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>());

        await taskItemRepository
            .DidNotReceive()
            .GetByProjectAsync(
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>());
    }

    private static GetProjectTasksHandler CreateHandler(
        IProjectRepository projectRepository,
        IProjectMemberRepository projectMemberRepository,
        ITaskItemRepository taskItemRepository,
        ICurrentUser currentUser)
    {
        var projectAccessPolicy =
            new ProjectAccessPolicy(
                projectMemberRepository,
                currentUser);

        return new GetProjectTasksHandler(
            projectRepository,
            taskItemRepository,
            projectAccessPolicy);
    }

    private static DateTimeOffset CreateUtcTime()
    {
        return new DateTimeOffset(
            2026,
            9,
            3,
            12,
            0,
            0,
            TimeSpan.Zero);
    }
}