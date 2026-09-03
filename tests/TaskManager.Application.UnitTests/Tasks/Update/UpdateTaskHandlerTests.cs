using NSubstitute;
using TaskManager.Application.Abstractions.Authentication;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Abstractions.Time;
using TaskManager.Application.Common.Authorization;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Tasks.Update;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using TaskManager.Domain.Exceptions;
using Xunit;

namespace TaskManager.Application.UnitTests.Tasks.Update;

public sealed class UpdateTaskHandlerTests
{
    [Fact]
    public async Task HandleAsyncWhenCurrentUserIsOwnerUpdatesTask()
    {
        var projectRepository =
            Substitute.For<IProjectRepository>();

        var projectMemberRepository =
            Substitute.For<IProjectMemberRepository>();

        var taskItemRepository =
            Substitute.For<ITaskItemRepository>();

        var unitOfWork =
            Substitute.For<IUnitOfWork>();

        var currentUser =
            Substitute.For<ICurrentUser>();

        var clock =
            Substitute.For<IClock>();

        var createdAtUtc =
            CreateUtcTime();

        var changedAtUtc =
            createdAtUtc.AddHours(1);

        var ownerId =
            Guid.NewGuid();

        var project =
            Project.Create(
                ownerId,
                "Update Task Project",
                null,
                createdAtUtc);

        var taskItem =
            TaskItem.Create(
                projectId: project.Id,
                createdByUserId: ownerId,
                title: "Old title",
                description: "Old description",
                priority: TaskPriority.Low,
                dueDateUtc: createdAtUtc.AddDays(1),
                createdAtUtc: createdAtUtc);

        currentUser.UserId.Returns(
            ownerId);

        clock.UtcNow.Returns(
            changedAtUtc);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        projectRepository
            .GetByIdAsync(
                project.Id,
                cancellationToken)
            .Returns(project);

        taskItemRepository
            .GetByIdAsync(
                taskItem.Id,
                cancellationToken)
            .Returns(taskItem);

        unitOfWork
            .SaveChangesAsync(
                cancellationToken)
            .Returns(1);

        var handler =
            CreateHandler(
                projectRepository,
                projectMemberRepository,
                taskItemRepository,
                unitOfWork,
                currentUser,
                clock);

        var command =
            new UpdateTaskCommand(
                ProjectId: project.Id,
                TaskItemId: taskItem.Id,
                Title: "Updated title",
                Description: null,
                Priority: TaskPriority.Critical,
                DueDateUtc: null);

        var result =
            await handler.HandleAsync(
                command,
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
            "Updated title",
            result.Title);

        Assert.Null(
            result.Description);

        Assert.Equal(
            TaskItemStatus.Backlog,
            result.Status);

        Assert.Equal(
            TaskPriority.Critical,
            result.Priority);

        Assert.Null(
            result.DueDateUtc);

        Assert.Equal(
            createdAtUtc,
            result.CreatedAtUtc);

        Assert.Equal(
            changedAtUtc,
            result.UpdatedAtUtc);

        Assert.Null(
            result.CompletedAtUtc);

        await projectMemberRepository
            .DidNotReceive()
            .GetByProjectAndUserAsync(
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>());

        await unitOfWork
            .Received(1)
            .SaveChangesAsync(
                cancellationToken);
    }

    [Fact]
    public async Task HandleAsyncWhenCurrentUserIsActiveMemberUpdatesTask()
    {
        var projectRepository =
            Substitute.For<IProjectRepository>();

        var projectMemberRepository =
            Substitute.For<IProjectMemberRepository>();

        var taskItemRepository =
            Substitute.For<ITaskItemRepository>();

        var unitOfWork =
            Substitute.For<IUnitOfWork>();

        var currentUser =
            Substitute.For<ICurrentUser>();

        var clock =
            Substitute.For<IClock>();

        var createdAtUtc =
            CreateUtcTime();

        var changedAtUtc =
            createdAtUtc.AddHours(1);

        var ownerId =
            Guid.NewGuid();

        var memberId =
            Guid.NewGuid();

        var project =
            Project.Create(
                ownerId,
                "Member Update Project",
                null,
                createdAtUtc);

        var membership =
            ProjectMember.Create(
                project.Id,
                memberId,
                ProjectMemberRole.Member,
                createdAtUtc);

        var taskItem =
            TaskItem.Create(
                projectId: project.Id,
                createdByUserId: ownerId,
                title: "Member editable task",
                description: null,
                priority: TaskPriority.Medium,
                dueDateUtc: null,
                createdAtUtc: createdAtUtc);

        currentUser.UserId.Returns(
            memberId);

        clock.UtcNow.Returns(
            changedAtUtc);

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
            .GetByIdAsync(
                taskItem.Id,
                cancellationToken)
            .Returns(taskItem);

        unitOfWork
            .SaveChangesAsync(
                cancellationToken)
            .Returns(1);

        var handler =
            CreateHandler(
                projectRepository,
                projectMemberRepository,
                taskItemRepository,
                unitOfWork,
                currentUser,
                clock);

        var command =
            new UpdateTaskCommand(
                ProjectId: project.Id,
                TaskItemId: taskItem.Id,
                Title: "Updated by member",
                Description: null,
                Priority: TaskPriority.Medium,
                DueDateUtc: null);

        var result =
            await handler.HandleAsync(
                command,
                cancellationToken);

        Assert.Equal(
            "Updated by member",
            result.Title);

        Assert.Equal(
            changedAtUtc,
            result.UpdatedAtUtc);

        await projectMemberRepository
            .Received(1)
            .GetByProjectAndUserAsync(
                project.Id,
                memberId,
                cancellationToken);

        await unitOfWork
            .Received(1)
            .SaveChangesAsync(
                cancellationToken);
    }

    [Fact]
    public async Task HandleAsyncWhenCurrentUserIsRemovedMemberThrowsNotFoundException()
    {
        var projectRepository =
            Substitute.For<IProjectRepository>();

        var projectMemberRepository =
            Substitute.For<IProjectMemberRepository>();

        var taskItemRepository =
            Substitute.For<ITaskItemRepository>();

        var unitOfWork =
            Substitute.For<IUnitOfWork>();

        var currentUser =
            Substitute.For<ICurrentUser>();

        var clock =
            Substitute.For<IClock>();

        var createdAtUtc =
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
                createdAtUtc);

        var membership =
            ProjectMember.Create(
                project.Id,
                memberId,
                ProjectMemberRole.Member,
                createdAtUtc);

        membership.Remove(
            createdAtUtc.AddMinutes(1));

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
                unitOfWork,
                currentUser,
                clock);

        var command =
            new UpdateTaskCommand(
                project.Id,
                Guid.NewGuid(),
                "Updated task",
                null,
                TaskPriority.Medium,
                null);

        var exception =
            await Assert.ThrowsAsync<ApplicationNotFoundException>(
                () => handler.HandleAsync(
                    command,
                    cancellationToken));

        Assert.Equal(
            "Project was not found.",
            exception.Message);

        await taskItemRepository
            .DidNotReceive()
            .GetByIdAsync(
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>());

        await unitOfWork
            .DidNotReceive()
            .SaveChangesAsync(
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

        var unitOfWork =
            Substitute.For<IUnitOfWork>();

        var currentUser =
            Substitute.For<ICurrentUser>();

        var clock =
            Substitute.For<IClock>();

        var createdAtUtc =
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
                createdAtUtc);

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
                unitOfWork,
                currentUser,
                clock);

        var command =
            new UpdateTaskCommand(
                project.Id,
                Guid.NewGuid(),
                "Updated task",
                null,
                TaskPriority.Medium,
                null);

        var exception =
            await Assert.ThrowsAsync<ApplicationNotFoundException>(
                () => handler.HandleAsync(
                    command,
                    cancellationToken));

        Assert.Equal(
            "Project was not found.",
            exception.Message);

        await taskItemRepository
            .DidNotReceive()
            .GetByIdAsync(
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>());

        await unitOfWork
            .DidNotReceive()
            .SaveChangesAsync(
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

        var unitOfWork =
            Substitute.For<IUnitOfWork>();

        var currentUser =
            Substitute.For<ICurrentUser>();

        var clock =
            Substitute.For<IClock>();

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
                unitOfWork,
                currentUser,
                clock);

        var command =
            new UpdateTaskCommand(
                projectId,
                Guid.NewGuid(),
                "Updated task",
                null,
                TaskPriority.Medium,
                null);

        var exception =
            await Assert.ThrowsAsync<ApplicationNotFoundException>(
                () => handler.HandleAsync(
                    command,
                    cancellationToken));

        Assert.Equal(
            "Project was not found.",
            exception.Message);

        await taskItemRepository
            .DidNotReceive()
            .GetByIdAsync(
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>());

        await unitOfWork
            .DidNotReceive()
            .SaveChangesAsync(
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsyncWhenProjectIsArchivedThrowsConflictException()
    {
        var projectRepository =
            Substitute.For<IProjectRepository>();

        var projectMemberRepository =
            Substitute.For<IProjectMemberRepository>();

        var taskItemRepository =
            Substitute.For<ITaskItemRepository>();

        var unitOfWork =
            Substitute.For<IUnitOfWork>();

        var currentUser =
            Substitute.For<ICurrentUser>();

        var clock =
            Substitute.For<IClock>();

        var createdAtUtc =
            CreateUtcTime();

        var ownerId =
            Guid.NewGuid();

        var project =
            Project.Create(
                ownerId,
                "Archived Project",
                null,
                createdAtUtc);

        project.Archive(
            createdAtUtc.AddMinutes(1));

        currentUser.UserId.Returns(
            ownerId);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        projectRepository
            .GetByIdAsync(
                project.Id,
                cancellationToken)
            .Returns(project);

        var handler =
            CreateHandler(
                projectRepository,
                projectMemberRepository,
                taskItemRepository,
                unitOfWork,
                currentUser,
                clock);

        var command =
            new UpdateTaskCommand(
                project.Id,
                Guid.NewGuid(),
                "Updated task",
                null,
                TaskPriority.Medium,
                null);

        var exception =
            await Assert.ThrowsAsync<ApplicationConflictException>(
                () => handler.HandleAsync(
                    command,
                    cancellationToken));

        Assert.Equal(
            "Cannot update tasks in an archived project.",
            exception.Message);

        await taskItemRepository
            .DidNotReceive()
            .GetByIdAsync(
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>());

        await unitOfWork
            .DidNotReceive()
            .SaveChangesAsync(
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

        var unitOfWork =
            Substitute.For<IUnitOfWork>();

        var currentUser =
            Substitute.For<ICurrentUser>();

        var clock =
            Substitute.For<IClock>();

        var createdAtUtc =
            CreateUtcTime();

        var ownerId =
            Guid.NewGuid();

        var taskItemId =
            Guid.NewGuid();

        var project =
            Project.Create(
                ownerId,
                "Task Missing Project",
                null,
                createdAtUtc);

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
            .GetByIdAsync(
                taskItemId,
                cancellationToken)
            .Returns((TaskItem?)null);

        var handler =
            CreateHandler(
                projectRepository,
                projectMemberRepository,
                taskItemRepository,
                unitOfWork,
                currentUser,
                clock);

        var command =
            new UpdateTaskCommand(
                project.Id,
                taskItemId,
                "Updated task",
                null,
                TaskPriority.Medium,
                null);

        var exception =
            await Assert.ThrowsAsync<ApplicationNotFoundException>(
                () => handler.HandleAsync(
                    command,
                    cancellationToken));

        Assert.Equal(
            "Task was not found.",
            exception.Message);

        await unitOfWork
            .DidNotReceive()
            .SaveChangesAsync(
                Arg.Any<CancellationToken>());
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

        var unitOfWork =
            Substitute.For<IUnitOfWork>();

        var currentUser =
            Substitute.For<ICurrentUser>();

        var clock =
            Substitute.For<IClock>();

        var createdAtUtc =
            CreateUtcTime();

        var ownerId =
            Guid.NewGuid();

        var project =
            Project.Create(
                ownerId,
                "Requested Project",
                null,
                createdAtUtc);

        var anotherProject =
            Project.Create(
                ownerId,
                "Another Project",
                null,
                createdAtUtc);

        var taskItem =
            TaskItem.Create(
                projectId: anotherProject.Id,
                createdByUserId: ownerId,
                title: "Another project task",
                description: null,
                priority: TaskPriority.Medium,
                dueDateUtc: null,
                createdAtUtc: createdAtUtc);

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
            .GetByIdAsync(
                taskItem.Id,
                cancellationToken)
            .Returns(taskItem);

        var handler =
            CreateHandler(
                projectRepository,
                projectMemberRepository,
                taskItemRepository,
                unitOfWork,
                currentUser,
                clock);

        var command =
            new UpdateTaskCommand(
                project.Id,
                taskItem.Id,
                "Updated task",
                null,
                TaskPriority.High,
                null);

        var exception =
            await Assert.ThrowsAsync<ApplicationNotFoundException>(
                () => handler.HandleAsync(
                    command,
                    cancellationToken));

        Assert.Equal(
            "Task was not found.",
            exception.Message);

        await unitOfWork
            .DidNotReceive()
            .SaveChangesAsync(
                Arg.Any<CancellationToken>());
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

        var unitOfWork =
            Substitute.For<IUnitOfWork>();

        var currentUser =
            Substitute.For<ICurrentUser>();

        var clock =
            Substitute.For<IClock>();

        var handler =
            CreateHandler(
                projectRepository,
                projectMemberRepository,
                taskItemRepository,
                unitOfWork,
                currentUser,
                clock);

        var command =
            new UpdateTaskCommand(
                Guid.Empty,
                Guid.NewGuid(),
                "Updated task",
                null,
                TaskPriority.Medium,
                null);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        var exception =
            await Assert.ThrowsAsync<ApplicationValidationException>(
                () => handler.HandleAsync(
                    command,
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

        var unitOfWork =
            Substitute.For<IUnitOfWork>();

        var currentUser =
            Substitute.For<ICurrentUser>();

        var clock =
            Substitute.For<IClock>();

        var handler =
            CreateHandler(
                projectRepository,
                projectMemberRepository,
                taskItemRepository,
                unitOfWork,
                currentUser,
                clock);

        var command =
            new UpdateTaskCommand(
                Guid.NewGuid(),
                Guid.Empty,
                "Updated task",
                null,
                TaskPriority.Medium,
                null);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        var exception =
            await Assert.ThrowsAsync<ApplicationValidationException>(
                () => handler.HandleAsync(
                    command,
                    cancellationToken));

        Assert.Equal(
            "Task identifier cannot be empty.",
            exception.Message);

        await projectRepository
            .DidNotReceive()
            .GetByIdAsync(
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsyncWhenPriorityIsInvalidThrowsValidationException()
    {
        var projectRepository =
            Substitute.For<IProjectRepository>();

        var projectMemberRepository =
            Substitute.For<IProjectMemberRepository>();

        var taskItemRepository =
            Substitute.For<ITaskItemRepository>();

        var unitOfWork =
            Substitute.For<IUnitOfWork>();

        var currentUser =
            Substitute.For<ICurrentUser>();

        var clock =
            Substitute.For<IClock>();

        var handler =
            CreateHandler(
                projectRepository,
                projectMemberRepository,
                taskItemRepository,
                unitOfWork,
                currentUser,
                clock);

        var command =
            new UpdateTaskCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Updated task",
                null,
                (TaskPriority)999,
                null);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        var exception =
            await Assert.ThrowsAsync<ApplicationValidationException>(
                () => handler.HandleAsync(
                    command,
                    cancellationToken));

        Assert.Equal(
            "Unsupported task priority: 999.",
            exception.Message);

        await projectRepository
            .DidNotReceive()
            .GetByIdAsync(
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsyncWhenTaskIsCompletedThrowsDomainConflictException()
    {
        var projectRepository =
            Substitute.For<IProjectRepository>();

        var projectMemberRepository =
            Substitute.For<IProjectMemberRepository>();

        var taskItemRepository =
            Substitute.For<ITaskItemRepository>();

        var unitOfWork =
            Substitute.For<IUnitOfWork>();

        var currentUser =
            Substitute.For<ICurrentUser>();

        var clock =
            Substitute.For<IClock>();

        var createdAtUtc =
            CreateUtcTime();

        var ownerId =
            Guid.NewGuid();

        var project =
            Project.Create(
                ownerId,
                "Completed Task Project",
                null,
                createdAtUtc);

        var taskItem =
            TaskItem.Create(
                projectId: project.Id,
                createdByUserId: ownerId,
                title: "Completed task",
                description: null,
                priority: TaskPriority.Medium,
                dueDateUtc: null,
                createdAtUtc: createdAtUtc);

        taskItem.ChangeStatus(
            TaskItemStatus.Todo,
            createdAtUtc.AddMinutes(1));

        taskItem.ChangeStatus(
            TaskItemStatus.InProgress,
            createdAtUtc.AddMinutes(2));

        taskItem.ChangeStatus(
            TaskItemStatus.Review,
            createdAtUtc.AddMinutes(3));

        taskItem.ChangeStatus(
            TaskItemStatus.Completed,
            createdAtUtc.AddMinutes(4));

        currentUser.UserId.Returns(
            ownerId);

        clock.UtcNow.Returns(
            createdAtUtc.AddMinutes(5));

        var cancellationToken =
            TestContext.Current.CancellationToken;

        projectRepository
            .GetByIdAsync(
                project.Id,
                cancellationToken)
            .Returns(project);

        taskItemRepository
            .GetByIdAsync(
                taskItem.Id,
                cancellationToken)
            .Returns(taskItem);

        var handler =
            CreateHandler(
                projectRepository,
                projectMemberRepository,
                taskItemRepository,
                unitOfWork,
                currentUser,
                clock);

        var command =
            new UpdateTaskCommand(
                project.Id,
                taskItem.Id,
                "Cannot update",
                null,
                TaskPriority.High,
                null);

        var exception =
            await Assert.ThrowsAsync<DomainConflictException>(
                () => handler.HandleAsync(
                    command,
                    cancellationToken));

        Assert.Equal(
            "Completed or cancelled task cannot be modified.",
            exception.Message);

        await unitOfWork
            .DidNotReceive()
            .SaveChangesAsync(
                Arg.Any<CancellationToken>());
    }

    private static UpdateTaskHandler CreateHandler(
        IProjectRepository projectRepository,
        IProjectMemberRepository projectMemberRepository,
        ITaskItemRepository taskItemRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IClock clock)
    {
        var projectAccessPolicy =
            new ProjectAccessPolicy(
                projectMemberRepository,
                currentUser);

        return new UpdateTaskHandler(
            projectRepository,
            taskItemRepository,
            unitOfWork,
            projectAccessPolicy,
            clock);
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