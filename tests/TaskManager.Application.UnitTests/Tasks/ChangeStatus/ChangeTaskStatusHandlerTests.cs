using NSubstitute;
using TaskManager.Application.Abstractions.Authentication;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Abstractions.Time;
using TaskManager.Application.Common.Authorization;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Tasks.ChangeStatus;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using TaskManager.Domain.Exceptions;
using Xunit;

namespace TaskManager.Application.UnitTests.Tasks.ChangeStatus;

public sealed class ChangeTaskStatusHandlerTests
{
    [Fact]
    public async Task HandleAsyncWhenCurrentUserIsOwnerChangesStatus()
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
                "Status Project",
                null,
                createdAtUtc);

        var taskItem =
            CreateTask(
                project.Id,
                ownerId,
                createdAtUtc);

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
            new ChangeTaskStatusCommand(
                ProjectId: project.Id,
                TaskItemId: taskItem.Id,
                Status: TaskItemStatus.Todo);

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
            TaskItemStatus.Todo,
            result.Status);

        Assert.Equal(
            changedAtUtc,
            result.UpdatedAtUtc);

        Assert.Null(
            result.CompletedAtUtc);

        Assert.Equal(
            TaskItemStatus.Todo,
            taskItem.Status);

        await projectMemberRepository
            .DidNotReceive()
            .GetByProjectAndUserAsync(
                project.Id,
                ownerId,
                Arg.Any<CancellationToken>());

        await unitOfWork
            .Received(1)
            .SaveChangesAsync(
                cancellationToken);
    }

    [Fact]
    public async Task HandleAsyncWhenChangingToCompletedSetsCompletedAtUtc()
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
                "Complete Task Project",
                null,
                createdAtUtc);

        var taskItem =
            CreateTask(
                project.Id,
                ownerId,
                createdAtUtc);

        taskItem.ChangeStatus(
            TaskItemStatus.Todo,
            createdAtUtc.AddMinutes(1));

        taskItem.ChangeStatus(
            TaskItemStatus.InProgress,
            createdAtUtc.AddMinutes(2));

        taskItem.ChangeStatus(
            TaskItemStatus.Review,
            createdAtUtc.AddMinutes(3));

        var completedAtUtc =
            createdAtUtc.AddMinutes(4);

        currentUser.UserId.Returns(
            ownerId);

        clock.UtcNow.Returns(
            completedAtUtc);

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
            new ChangeTaskStatusCommand(
                project.Id,
                taskItem.Id,
                TaskItemStatus.Completed);

        var result =
            await handler.HandleAsync(
                command,
                cancellationToken);

        Assert.Equal(
            TaskItemStatus.Completed,
            result.Status);

        Assert.Equal(
            completedAtUtc,
            result.UpdatedAtUtc);

        Assert.Equal(
            completedAtUtc,
            result.CompletedAtUtc);

        Assert.Equal(
            completedAtUtc,
            taskItem.CompletedAtUtc);

        await unitOfWork
            .Received(1)
            .SaveChangesAsync(
                cancellationToken);
    }

    [Fact]
    public async Task HandleAsyncWhenCurrentUserIsActiveMemberChangesStatus()
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
                "Member Status Project",
                null,
                createdAtUtc);

        var membership =
            ProjectMember.Create(
                project.Id,
                memberId,
                ProjectMemberRole.Member,
                createdAtUtc);

        var taskItem =
            CreateTask(
                project.Id,
                ownerId,
                createdAtUtc);

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
            new ChangeTaskStatusCommand(
                project.Id,
                taskItem.Id,
                TaskItemStatus.Todo);

        var result =
            await handler.HandleAsync(
                command,
                cancellationToken);

        Assert.Equal(
            TaskItemStatus.Todo,
            result.Status);

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
            new ChangeTaskStatusCommand(
                project.Id,
                Guid.NewGuid(),
                TaskItemStatus.Todo);

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
                "Private Status Project",
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
            new ChangeTaskStatusCommand(
                project.Id,
                Guid.NewGuid(),
                TaskItemStatus.Todo);

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
            new ChangeTaskStatusCommand(
                projectId,
                Guid.NewGuid(),
                TaskItemStatus.Todo);

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
                "Archived Status Project",
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
            new ChangeTaskStatusCommand(
                project.Id,
                Guid.NewGuid(),
                TaskItemStatus.Todo);

        var exception =
            await Assert.ThrowsAsync<ApplicationConflictException>(
                () => handler.HandleAsync(
                    command,
                    cancellationToken));

        Assert.Equal(
            "Cannot change task status in an archived project.",
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
                "Missing Task Project",
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
            new ChangeTaskStatusCommand(
                project.Id,
                taskItemId,
                TaskItemStatus.Todo);

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
                "Correct Project",
                null,
                createdAtUtc);

        var anotherProject =
            Project.Create(
                ownerId,
                "Another Project",
                null,
                createdAtUtc);

        var taskItem =
            CreateTask(
                anotherProject.Id,
                ownerId,
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
            new ChangeTaskStatusCommand(
                project.Id,
                taskItem.Id,
                TaskItemStatus.Todo);

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
    public async Task HandleAsyncWhenTransitionIsInvalidThrowsDomainConflictException()
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
                "Invalid Transition Project",
                null,
                createdAtUtc);

        var taskItem =
            CreateTask(
                project.Id,
                ownerId,
                createdAtUtc);

        currentUser.UserId.Returns(
            ownerId);

        clock.UtcNow.Returns(
            createdAtUtc.AddHours(1));

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
            new ChangeTaskStatusCommand(
                project.Id,
                taskItem.Id,
                TaskItemStatus.Completed);

        var exception =
            await Assert.ThrowsAsync<DomainConflictException>(
                () => handler.HandleAsync(
                    command,
                    cancellationToken));

        Assert.Equal(
            "Cannot change task status from Backlog to Completed.",
            exception.Message);

        Assert.Equal(
            TaskItemStatus.Backlog,
            taskItem.Status);

        await unitOfWork
            .DidNotReceive()
            .SaveChangesAsync(
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsyncWhenProjectIdIsEmptyThrowsValidationException()
    {
        var handler =
            CreateHandler(
                Substitute.For<IProjectRepository>(),
                Substitute.For<IProjectMemberRepository>(),
                Substitute.For<ITaskItemRepository>(),
                Substitute.For<IUnitOfWork>(),
                Substitute.For<ICurrentUser>(),
                Substitute.For<IClock>());

        var command =
            new ChangeTaskStatusCommand(
                Guid.Empty,
                Guid.NewGuid(),
                TaskItemStatus.Todo);

        var exception =
            await Assert.ThrowsAsync<ApplicationValidationException>(
                () => handler.HandleAsync(
                    command,
                    TestContext.Current.CancellationToken));

        Assert.Equal(
            "Project identifier cannot be empty.",
            exception.Message);

        Assert.Equal(
            nameof(command.ProjectId),
            exception.ParameterName);
    }

    [Fact]
    public async Task HandleAsyncWhenTaskItemIdIsEmptyThrowsValidationException()
    {
        var handler =
            CreateHandler(
                Substitute.For<IProjectRepository>(),
                Substitute.For<IProjectMemberRepository>(),
                Substitute.For<ITaskItemRepository>(),
                Substitute.For<IUnitOfWork>(),
                Substitute.For<ICurrentUser>(),
                Substitute.For<IClock>());

        var command =
            new ChangeTaskStatusCommand(
                Guid.NewGuid(),
                Guid.Empty,
                TaskItemStatus.Todo);

        var exception =
            await Assert.ThrowsAsync<ApplicationValidationException>(
                () => handler.HandleAsync(
                    command,
                    TestContext.Current.CancellationToken));

        Assert.Equal(
            "Task identifier cannot be empty.",
            exception.Message);

        Assert.Equal(
            nameof(command.TaskItemId),
            exception.ParameterName);
    }

    [Fact]
    public async Task HandleAsyncWhenStatusIsUnsupportedThrowsValidationException()
    {
        var handler =
            CreateHandler(
                Substitute.For<IProjectRepository>(),
                Substitute.For<IProjectMemberRepository>(),
                Substitute.For<ITaskItemRepository>(),
                Substitute.For<IUnitOfWork>(),
                Substitute.For<ICurrentUser>(),
                Substitute.For<IClock>());

        var unsupportedStatus =
            (TaskItemStatus)999;

        var command =
            new ChangeTaskStatusCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                unsupportedStatus);

        var exception =
            await Assert.ThrowsAsync<ApplicationValidationException>(
                () => handler.HandleAsync(
                    command,
                    TestContext.Current.CancellationToken));

        Assert.Equal(
            $"Unsupported task status: {unsupportedStatus}.",
            exception.Message);

        Assert.Equal(
            nameof(command.Status),
            exception.ParameterName);
    }

    private static ChangeTaskStatusHandler CreateHandler(
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

        return new ChangeTaskStatusHandler(
            projectRepository,
            taskItemRepository,
            unitOfWork,
            projectAccessPolicy,
            clock);
    }

    private static TaskItem CreateTask(
        Guid projectId,
        Guid createdByUserId,
        DateTimeOffset createdAtUtc)
    {
        return TaskItem.Create(
            projectId: projectId,
            createdByUserId: createdByUserId,
            title: "Task status test",
            description: null,
            priority: TaskPriority.Medium,
            dueDateUtc: null,
            createdAtUtc: createdAtUtc);
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