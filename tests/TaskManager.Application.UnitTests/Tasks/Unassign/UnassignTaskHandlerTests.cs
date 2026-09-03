using NSubstitute;
using TaskManager.Application.Abstractions.Authentication;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Abstractions.Time;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Tasks.Unassign;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using TaskManager.Domain.Exceptions;
using Xunit;
using TaskManager.Application.Common.Authorization;

namespace TaskManager.Application.UnitTests.Tasks.Unassign;

public sealed class UnassignTaskHandlerTests
{
    [Fact]
    public async Task HandleAsyncWhenCurrentUserIsOwnerUnassignsTask()
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

        var assignedAtUtc =
            createdAtUtc.AddMinutes(1);

        var changedAtUtc =
            createdAtUtc.AddMinutes(2);

        var ownerId =
            Guid.NewGuid();

        var assigneeId =
            Guid.NewGuid();

        var project =
            Project.Create(
                ownerId,
                "Unassign Task Project",
                null,
                createdAtUtc);

        var taskItem =
            TaskItem.Create(
                projectId: project.Id,
                createdByUserId: ownerId,
                title: "Assigned task",
                description: null,
                priority: TaskPriority.Medium,
                dueDateUtc: null,
                createdAtUtc: createdAtUtc);

        taskItem.Assign(
            assigneeId,
            assignedAtUtc);

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
            new UnassignTaskCommand(
                ProjectId: project.Id,
                TaskItemId: taskItem.Id);

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
            changedAtUtc,
            result.UpdatedAtUtc);

        Assert.Null(
            taskItem.AssigneeId);

        Assert.Equal(
            changedAtUtc,
            taskItem.UpdatedAtUtc);

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
    public async Task HandleAsyncWhenCurrentUserIsActiveMemberUnassignsTask()
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

        var assignedAtUtc =
            createdAtUtc.AddMinutes(1);

        var changedAtUtc =
            createdAtUtc.AddMinutes(2);

        var ownerId =
            Guid.NewGuid();

        var memberId =
            Guid.NewGuid();

        var assigneeId =
            Guid.NewGuid();

        var project =
            Project.Create(
                ownerId,
                "Member Unassign Project",
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
                project.Id,
                ownerId,
                "Member unassignable task",
                null,
                TaskPriority.Medium,
                null,
                createdAtUtc);

        taskItem.Assign(
            assigneeId,
            assignedAtUtc);

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
            new UnassignTaskCommand(
                project.Id,
                taskItem.Id);

        var result =
            await handler.HandleAsync(
                command,
                cancellationToken);

        Assert.Null(
            taskItem.AssigneeId);

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
    public async Task HandleAsyncWhenTaskAlreadyUnassignedRemainsUnassigned()
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
                "Already Unassigned Project",
                null,
                createdAtUtc);

        var taskItem =
            TaskItem.Create(
                project.Id,
                ownerId,
                "Already unassigned task",
                null,
                TaskPriority.Medium,
                null,
                createdAtUtc);

        currentUser.UserId.Returns(
            ownerId);

        clock.UtcNow.Returns(
            createdAtUtc.AddMinutes(1));

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
            new UnassignTaskCommand(
                project.Id,
                taskItem.Id);

        var result =
            await handler.HandleAsync(
                command,
                cancellationToken);

        Assert.Null(
            taskItem.AssigneeId);

        Assert.Null(
            taskItem.UpdatedAtUtc);

        Assert.Null(
            result.UpdatedAtUtc);

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
            new UnassignTaskCommand(
                project.Id,
                Guid.NewGuid());

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
                "Private Unassign Project",
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
            new UnassignTaskCommand(
                project.Id,
                Guid.NewGuid());

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
            new UnassignTaskCommand(
                projectId,
                Guid.NewGuid());

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
                "Archived Unassign Project",
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
            new UnassignTaskCommand(
                project.Id,
                Guid.NewGuid());

        var exception =
            await Assert.ThrowsAsync<ApplicationConflictException>(
                () => handler.HandleAsync(
                    command,
                    cancellationToken));

        Assert.Equal(
            "Cannot unassign tasks in an archived project.",
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
            new UnassignTaskCommand(
                project.Id,
                taskItemId);

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
                anotherProject.Id,
                ownerId,
                "Another project task",
                null,
                TaskPriority.Medium,
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
            new UnassignTaskCommand(
                project.Id,
                taskItem.Id);

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
            new UnassignTaskCommand(
                Guid.Empty,
                Guid.NewGuid());

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
            new UnassignTaskCommand(
                Guid.NewGuid(),
                Guid.Empty);

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

        var assigneeId =
            Guid.NewGuid();

        var project =
            Project.Create(
                ownerId,
                "Completed Task Project",
                null,
                createdAtUtc);

        var taskItem =
            TaskItem.Create(
                project.Id,
                ownerId,
                "Completed task",
                null,
                TaskPriority.Medium,
                null,
                createdAtUtc);

        taskItem.Assign(
            assigneeId,
            createdAtUtc.AddMinutes(1));

        taskItem.ChangeStatus(
            TaskItemStatus.Todo,
            createdAtUtc.AddMinutes(2));

        taskItem.ChangeStatus(
            TaskItemStatus.InProgress,
            createdAtUtc.AddMinutes(3));

        taskItem.ChangeStatus(
            TaskItemStatus.Review,
            createdAtUtc.AddMinutes(4));

        taskItem.ChangeStatus(
            TaskItemStatus.Completed,
            createdAtUtc.AddMinutes(5));

        currentUser.UserId.Returns(
            ownerId);

        clock.UtcNow.Returns(
            createdAtUtc.AddMinutes(6));

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
            new UnassignTaskCommand(
                project.Id,
                taskItem.Id);

        var exception =
            await Assert.ThrowsAsync<DomainConflictException>(
                () => handler.HandleAsync(
                    command,
                    cancellationToken));

        Assert.Equal(
            "Completed or cancelled task cannot be modified.",
            exception.Message);

        Assert.Equal(
            assigneeId,
            taskItem.AssigneeId);

        await unitOfWork
            .DidNotReceive()
            .SaveChangesAsync(
                Arg.Any<CancellationToken>());
    }

    private static UnassignTaskHandler CreateHandler(
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

        return new UnassignTaskHandler(
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