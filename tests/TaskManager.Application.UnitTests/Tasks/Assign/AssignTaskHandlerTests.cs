using NSubstitute;
using TaskManager.Application.Abstractions.Authentication;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Abstractions.Time;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Tasks.Assign;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using TaskManager.Domain.Exceptions;
using Xunit;
using TaskManager.Application.Common.Authorization;

namespace TaskManager.Application.UnitTests.Tasks.Assign;

public sealed class AssignTaskHandlerTests
{
    [Fact]
    public async Task HandleAsyncWhenCurrentUserIsOwnerAssignsTask()
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

        var assigneeId =
            Guid.NewGuid();

        var project =
            Project.Create(
                ownerId,
                "Assign Task Project",
                null,
                createdAtUtc);

        var assigneeMembership =
            ProjectMember.Create(
                project.Id,
                assigneeId,
                ProjectMemberRole.Member,
                createdAtUtc);

        var taskItem =
            TaskItem.Create(
                projectId: project.Id,
                createdByUserId: ownerId,
                title: "Task to assign",
                description: null,
                priority: TaskPriority.Medium,
                dueDateUtc: null,
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

        projectMemberRepository
            .GetByProjectAndUserAsync(
                project.Id,
                assigneeId,
                cancellationToken)
            .Returns(assigneeMembership);

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
            new AssignTaskCommand(
                ProjectId: project.Id,
                TaskItemId: taskItem.Id,
                AssigneeId: assigneeId);

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
            assigneeId,
            result.AssigneeId);

        Assert.Equal(
            changedAtUtc,
            result.UpdatedAtUtc);

        Assert.Equal(
            assigneeId,
            taskItem.AssigneeId);

        Assert.Equal(
            changedAtUtc,
            taskItem.UpdatedAtUtc);

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
    public async Task HandleAsyncWhenCurrentUserIsActiveMemberAssignsTask()
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

        var currentMemberId =
            Guid.NewGuid();

        var assigneeId =
            Guid.NewGuid();

        var project =
            Project.Create(
                ownerId,
                "Member Assign Project",
                null,
                createdAtUtc);

        var currentMembership =
            ProjectMember.Create(
                project.Id,
                currentMemberId,
                ProjectMemberRole.Member,
                createdAtUtc);

        var assigneeMembership =
            ProjectMember.Create(
                project.Id,
                assigneeId,
                ProjectMemberRole.Member,
                createdAtUtc);

        var taskItem =
            TaskItem.Create(
                projectId: project.Id,
                createdByUserId: ownerId,
                title: "Member assignable task",
                description: null,
                priority: TaskPriority.Medium,
                dueDateUtc: null,
                createdAtUtc: createdAtUtc);

        currentUser.UserId.Returns(
            currentMemberId);

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
                currentMemberId,
                cancellationToken)
            .Returns(currentMembership);

        projectMemberRepository
            .GetByProjectAndUserAsync(
                project.Id,
                assigneeId,
                cancellationToken)
            .Returns(assigneeMembership);

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
            new AssignTaskCommand(
                project.Id,
                taskItem.Id,
                assigneeId);

        var result =
            await handler.HandleAsync(
                command,
                cancellationToken);

        Assert.Equal(
            assigneeId,
            result.AssigneeId);

        Assert.Equal(
            assigneeId,
            taskItem.AssigneeId);

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

        var currentMemberId =
            Guid.NewGuid();

        var project =
            Project.Create(
                ownerId,
                "Removed Member Project",
                null,
                createdAtUtc);

        var currentMembership =
            ProjectMember.Create(
                project.Id,
                currentMemberId,
                ProjectMemberRole.Member,
                createdAtUtc);

        currentMembership.Remove(
            createdAtUtc.AddMinutes(1));

        currentUser.UserId.Returns(
            currentMemberId);

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
                currentMemberId,
                cancellationToken)
            .Returns(currentMembership);

        var handler =
            CreateHandler(
                projectRepository,
                projectMemberRepository,
                taskItemRepository,
                unitOfWork,
                currentUser,
                clock);

        var command =
            new AssignTaskCommand(
                project.Id,
                Guid.NewGuid(),
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
                "Private Assign Project",
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
            new AssignTaskCommand(
                project.Id,
                Guid.NewGuid(),
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
            new AssignTaskCommand(
                projectId,
                Guid.NewGuid(),
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
                "Archived Assign Project",
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
            new AssignTaskCommand(
                project.Id,
                Guid.NewGuid(),
                Guid.NewGuid());

        var exception =
            await Assert.ThrowsAsync<ApplicationConflictException>(
                () => handler.HandleAsync(
                    command,
                    cancellationToken));

        Assert.Equal(
            "Cannot assign tasks in an archived project.",
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
            new AssignTaskCommand(
                project.Id,
                taskItemId,
                Guid.NewGuid());

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
            new AssignTaskCommand(
                project.Id,
                taskItem.Id,
                Guid.NewGuid());

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
    public async Task HandleAsyncWhenAssigneeIsNotProjectMemberThrowsNotFoundException()
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
                "Missing Assignee Project",
                null,
                createdAtUtc);

        var taskItem =
            TaskItem.Create(
                project.Id,
                ownerId,
                "Assignable task",
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

        projectMemberRepository
            .GetByProjectAndUserAsync(
                project.Id,
                assigneeId,
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
            new AssignTaskCommand(
                project.Id,
                taskItem.Id,
                assigneeId);

        var exception =
            await Assert.ThrowsAsync<ApplicationNotFoundException>(
                () => handler.HandleAsync(
                    command,
                    cancellationToken));

        Assert.Equal(
            "Assignee was not found in the project.",
            exception.Message);

        Assert.Null(
            taskItem.AssigneeId);

        await unitOfWork
            .DidNotReceive()
            .SaveChangesAsync(
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsyncWhenAssigneeWasRemovedThrowsNotFoundException()
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
                "Removed Assignee Project",
                null,
                createdAtUtc);

        var assigneeMembership =
            ProjectMember.Create(
                project.Id,
                assigneeId,
                ProjectMemberRole.Member,
                createdAtUtc);

        assigneeMembership.Remove(
            createdAtUtc.AddMinutes(1));

        var taskItem =
            TaskItem.Create(
                project.Id,
                ownerId,
                "Assignable task",
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

        projectMemberRepository
            .GetByProjectAndUserAsync(
                project.Id,
                assigneeId,
                cancellationToken)
            .Returns(assigneeMembership);

        var handler =
            CreateHandler(
                projectRepository,
                projectMemberRepository,
                taskItemRepository,
                unitOfWork,
                currentUser,
                clock);

        var command =
            new AssignTaskCommand(
                project.Id,
                taskItem.Id,
                assigneeId);

        var exception =
            await Assert.ThrowsAsync<ApplicationNotFoundException>(
                () => handler.HandleAsync(
                    command,
                    cancellationToken));

        Assert.Equal(
            "Assignee was not found in the project.",
            exception.Message);

        Assert.Null(
            taskItem.AssigneeId);

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
            new AssignTaskCommand(
                Guid.Empty,
                Guid.NewGuid(),
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
            new AssignTaskCommand(
                Guid.NewGuid(),
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
            "Task identifier cannot be empty.",
            exception.Message);

        await projectRepository
            .DidNotReceive()
            .GetByIdAsync(
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsyncWhenAssigneeIdIsEmptyThrowsValidationException()
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
            new AssignTaskCommand(
                Guid.NewGuid(),
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
            "Assignee identifier cannot be empty.",
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

        var completedAtUtc =
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
                "Completed Task Project",
                null,
                createdAtUtc);

        var assigneeMembership =
            ProjectMember.Create(
                project.Id,
                assigneeId,
                ProjectMemberRole.Member,
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

        taskItem.ChangeStatus(
            TaskItemStatus.Todo,
            completedAtUtc);

        taskItem.ChangeStatus(
            TaskItemStatus.InProgress,
            completedAtUtc);

        taskItem.ChangeStatus(
            TaskItemStatus.Review,
            completedAtUtc);

        taskItem.ChangeStatus(
            TaskItemStatus.Completed,
            completedAtUtc);

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

        projectMemberRepository
            .GetByProjectAndUserAsync(
                project.Id,
                assigneeId,
                cancellationToken)
            .Returns(assigneeMembership);

        var handler =
            CreateHandler(
                projectRepository,
                projectMemberRepository,
                taskItemRepository,
                unitOfWork,
                currentUser,
                clock);

        var command =
            new AssignTaskCommand(
                project.Id,
                taskItem.Id,
                assigneeId);

        var exception =
            await Assert.ThrowsAsync<DomainConflictException>(
                () => handler.HandleAsync(
                    command,
                    cancellationToken));

        Assert.Equal(
            "Completed or cancelled task cannot be modified.",
            exception.Message);

        Assert.Null(
            taskItem.AssigneeId);

        await unitOfWork
            .DidNotReceive()
            .SaveChangesAsync(
                Arg.Any<CancellationToken>());
    }

    private static AssignTaskHandler CreateHandler(
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

        return new AssignTaskHandler(
            projectRepository,
            projectMemberRepository,
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