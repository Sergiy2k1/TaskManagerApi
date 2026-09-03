using NSubstitute;
using TaskManager.Application.Abstractions.Authentication;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Common.Authorization;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Tasks.GetById;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using Xunit;

namespace TaskManager.Application.UnitTests.Tasks.GetById;

public sealed class GetTaskByIdHandlerTests
{
    [Fact]
    public async Task HandleAsyncWhenCurrentUserIsOwnerReturnsTask()
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

        var dueDateUtc =
            now.AddDays(3);

        var taskItem =
            TaskItem.Create(
                projectId: project.Id,
                createdByUserId: ownerId,
                title: "Implement Get Task",
                description: "Return task by identifier",
                priority: TaskPriority.High,
                dueDateUtc: dueDateUtc,
                createdAtUtc: now);

        currentUser.UserId.Returns(
            ownerId);

        projectRepository
            .GetByIdAsync(
                project.Id,
                Arg.Any<CancellationToken>())
            .Returns(project);

        taskItemRepository
            .GetByIdAsync(
                taskItem.Id,
                Arg.Any<CancellationToken>())
            .Returns(taskItem);

        var handler =
            CreateHandler(
                projectRepository,
                projectMemberRepository,
                taskItemRepository,
                currentUser);

        var query =
            new GetTaskByIdQuery(
                ProjectId: project.Id,
                TaskItemId: taskItem.Id);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        var result =
            await handler.HandleAsync(
                query,
                cancellationToken);

        Assert.Equal(
            taskItem.Id,
            result.TaskItemId);

        Assert.Equal(
            project.Id,
            result.ProjectId);

        Assert.Equal(
            ownerId,
            result.CreatedByUserId);

        Assert.Null(
            result.AssigneeId);

        Assert.Equal(
            "Implement Get Task",
            result.Title);

        Assert.Equal(
            "Return task by identifier",
            result.Description);

        Assert.Equal(
            TaskItemStatus.Backlog,
            result.Status);

        Assert.Equal(
            TaskPriority.High,
            result.Priority);

        Assert.Equal(
            dueDateUtc,
            result.DueDateUtc);

        Assert.Equal(
            now,
            result.CreatedAtUtc);

        Assert.Null(
            result.UpdatedAtUtc);

        Assert.Null(
            result.CompletedAtUtc);

        await projectMemberRepository
            .DidNotReceive()
            .GetByProjectAndUserAsync(
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsyncWhenCurrentUserIsActiveMemberReturnsTask()
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
                title: "Visible Task",
                description: null,
                priority: TaskPriority.Medium,
                dueDateUtc: null,
                createdAtUtc: now);

        currentUser.UserId.Returns(
            memberId);

        projectRepository
            .GetByIdAsync(
                project.Id,
                Arg.Any<CancellationToken>())
            .Returns(project);

        projectMemberRepository
            .GetByProjectAndUserAsync(
                project.Id,
                memberId,
                Arg.Any<CancellationToken>())
            .Returns(membership);

        taskItemRepository
            .GetByIdAsync(
                taskItem.Id,
                Arg.Any<CancellationToken>())
            .Returns(taskItem);

        var handler =
            CreateHandler(
                projectRepository,
                projectMemberRepository,
                taskItemRepository,
                currentUser);

        var query =
            new GetTaskByIdQuery(
                project.Id,
                taskItem.Id);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        var result =
            await handler.HandleAsync(
                query,
                cancellationToken);

        Assert.Equal(
            taskItem.Id,
            result.TaskItemId);

        Assert.Equal(
            project.Id,
            result.ProjectId);

        Assert.Equal(
            "Visible Task",
            result.Title);

        await projectMemberRepository
            .Received(1)
            .GetByProjectAndUserAsync(
                project.Id,
                memberId,
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

        projectRepository
            .GetByIdAsync(
                project.Id,
                Arg.Any<CancellationToken>())
            .Returns(project);

        projectMemberRepository
            .GetByProjectAndUserAsync(
                project.Id,
                memberId,
                Arg.Any<CancellationToken>())
            .Returns(membership);

        var handler =
            CreateHandler(
                projectRepository,
                projectMemberRepository,
                taskItemRepository,
                currentUser);

        var query =
            new GetTaskByIdQuery(
                project.Id,
                Guid.NewGuid());

        var cancellationToken =
            TestContext.Current.CancellationToken;

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
            .GetByIdAsync(
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

        projectRepository
            .GetByIdAsync(
                project.Id,
                Arg.Any<CancellationToken>())
            .Returns(project);

        projectMemberRepository
            .GetByProjectAndUserAsync(
                project.Id,
                outsiderId,
                Arg.Any<CancellationToken>())
            .Returns((ProjectMember?)null);

        var handler =
            CreateHandler(
                projectRepository,
                projectMemberRepository,
                taskItemRepository,
                currentUser);

        var query =
            new GetTaskByIdQuery(
                project.Id,
                Guid.NewGuid());

        var cancellationToken =
            TestContext.Current.CancellationToken;

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
            .GetByIdAsync(
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

        projectRepository
            .GetByIdAsync(
                projectId,
                Arg.Any<CancellationToken>())
            .Returns((Project?)null);

        var handler =
            CreateHandler(
                projectRepository,
                projectMemberRepository,
                taskItemRepository,
                currentUser);

        var query =
            new GetTaskByIdQuery(
                projectId,
                Guid.NewGuid());

        var cancellationToken =
            TestContext.Current.CancellationToken;

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
            .GetByIdAsync(
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsyncWhenTaskDoesNotExistThrowsNotFoundException()
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
                "Task Lookup Project",
                null,
                now);

        var taskItemId =
            Guid.NewGuid();

        currentUser.UserId.Returns(
            ownerId);

        projectRepository
            .GetByIdAsync(
                project.Id,
                Arg.Any<CancellationToken>())
            .Returns(project);

        taskItemRepository
            .GetByIdAsync(
                taskItemId,
                Arg.Any<CancellationToken>())
            .Returns((TaskItem?)null);

        var handler =
            CreateHandler(
                projectRepository,
                projectMemberRepository,
                taskItemRepository,
                currentUser);

        var query =
            new GetTaskByIdQuery(
                project.Id,
                taskItemId);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        var exception =
            await Assert.ThrowsAsync<ApplicationNotFoundException>(
                () => handler.HandleAsync(
                    query,
                    cancellationToken));

        Assert.Equal(
            "Task was not found.",
            exception.Message);
    }

    [Fact]
    public async Task HandleAsyncWhenTaskBelongsToAnotherProjectThrowsNotFoundException()
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
                "Requested Project",
                null,
                now);

        var anotherProjectId =
            Guid.NewGuid();

        var taskItem =
            TaskItem.Create(
                projectId: anotherProjectId,
                createdByUserId: ownerId,
                title: "Other Project Task",
                description: null,
                priority: TaskPriority.Low,
                dueDateUtc: null,
                createdAtUtc: now);

        currentUser.UserId.Returns(
            ownerId);

        projectRepository
            .GetByIdAsync(
                project.Id,
                Arg.Any<CancellationToken>())
            .Returns(project);

        taskItemRepository
            .GetByIdAsync(
                taskItem.Id,
                Arg.Any<CancellationToken>())
            .Returns(taskItem);

        var handler =
            CreateHandler(
                projectRepository,
                projectMemberRepository,
                taskItemRepository,
                currentUser);

        var query =
            new GetTaskByIdQuery(
                project.Id,
                taskItem.Id);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        var exception =
            await Assert.ThrowsAsync<ApplicationNotFoundException>(
                () => handler.HandleAsync(
                    query,
                    cancellationToken));

        Assert.Equal(
            "Task was not found.",
            exception.Message);
    }

    [Fact]
    public async Task HandleAsyncWhenProjectIsArchivedStillReturnsTask()
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
                title: "Archived Task",
                description: null,
                priority: TaskPriority.Medium,
                dueDateUtc: null,
                createdAtUtc: now);

        project.Archive(
            now.AddMinutes(1));

        currentUser.UserId.Returns(
            ownerId);

        projectRepository
            .GetByIdAsync(
                project.Id,
                Arg.Any<CancellationToken>())
            .Returns(project);

        taskItemRepository
            .GetByIdAsync(
                taskItem.Id,
                Arg.Any<CancellationToken>())
            .Returns(taskItem);

        var handler =
            CreateHandler(
                projectRepository,
                projectMemberRepository,
                taskItemRepository,
                currentUser);

        var query =
            new GetTaskByIdQuery(
                project.Id,
                taskItem.Id);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        var result =
            await handler.HandleAsync(
                query,
                cancellationToken);

        Assert.Equal(
            taskItem.Id,
            result.TaskItemId);

        Assert.Equal(
            project.Id,
            result.ProjectId);
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
            new GetTaskByIdQuery(
                Guid.Empty,
                Guid.NewGuid());

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
    }

    [Fact]
    public async Task HandleAsyncWhenTaskItemIdIsEmptyThrowsValidationException()
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
            new GetTaskByIdQuery(
                Guid.NewGuid(),
                Guid.Empty);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        var exception =
            await Assert.ThrowsAsync<ApplicationValidationException>(
                () => handler.HandleAsync(
                    query,
                    cancellationToken));

        Assert.Equal(
            "Task identifier cannot be empty.",
            exception.Message);

        await projectRepository
            .DidNotReceive()
            .GetByIdAsync(
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>());

        await taskItemRepository
            .DidNotReceive()
            .GetByIdAsync(
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>());
    }

    private static GetTaskByIdHandler CreateHandler(
        IProjectRepository projectRepository,
        IProjectMemberRepository projectMemberRepository,
        ITaskItemRepository taskItemRepository,
        ICurrentUser currentUser)
    {
        var projectAccessPolicy =
            new ProjectAccessPolicy(
                projectMemberRepository,
                currentUser);

        return new GetTaskByIdHandler(
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