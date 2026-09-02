using NSubstitute;
using TaskManager.Application.Abstractions.Authentication;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Abstractions.Time;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Tasks.Create;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using Xunit;

namespace TaskManager.Application.UnitTests.Tasks.Create;

public sealed class CreateTaskHandlerTests
{
    [Fact]
    public async Task HandleAsyncWhenCurrentUserIsOwnerCreatesTask()
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

        var now =
            CreateUtcTime();

        var dueDateUtc =
            now.AddDays(2);

        var ownerId =
            Guid.NewGuid();

        var project =
            Project.Create(
                ownerId,
                "Task Manager",
                null,
                now);

        currentUser.UserId.Returns(ownerId);
        clock.UtcNow.Returns(now);

        projectRepository
            .GetByIdAsync(
                project.Id,
                Arg.Any<CancellationToken>())
            .Returns(project);

        unitOfWork
            .SaveChangesAsync(
                Arg.Any<CancellationToken>())
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
            new CreateTaskCommand(
                ProjectId: project.Id,
                Title: "Create authentication",
                Description: "Implement JWT authentication",
                Priority: TaskPriority.High,
                DueDateUtc: dueDateUtc);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        var result =
            await handler.HandleAsync(
                command,
                cancellationToken);

        Assert.Equal(
            project.Id,
            result.ProjectId);

        Assert.Equal(
            ownerId,
            result.CreatedByUserId);

        Assert.Null(
            result.AssigneeId);

        Assert.Equal(
            "Create authentication",
            result.Title);

        Assert.Equal(
            "Implement JWT authentication",
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

        taskItemRepository
            .Received(1)
            .Add(
                Arg.Is<TaskItem>(
                    taskItem =>
                        taskItem.Id == result.TaskItemId &&
                        taskItem.ProjectId == project.Id &&
                        taskItem.CreatedByUserId == ownerId &&
                        taskItem.AssigneeId == null &&
                        taskItem.Title == "Create authentication" &&
                        taskItem.Description ==
                            "Implement JWT authentication" &&
                        taskItem.Status ==
                            TaskItemStatus.Backlog &&
                        taskItem.Priority ==
                            TaskPriority.High &&
                        taskItem.DueDateUtc ==
                            dueDateUtc &&
                        taskItem.CreatedAtUtc ==
                            now));

        await unitOfWork
            .Received(1)
            .SaveChangesAsync(
                cancellationToken);

        await projectMemberRepository
            .DidNotReceive()
            .GetByProjectAndUserAsync(
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsyncWhenCurrentUserIsActiveMemberCreatesTask()
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

        var now =
            CreateUtcTime();

        var ownerId =
            Guid.NewGuid();

        var memberId =
            Guid.NewGuid();

        var project =
            Project.Create(
                ownerId,
                "Member Tasks",
                null,
                now);

        var membership =
            ProjectMember.Create(
                project.Id,
                memberId,
                ProjectMemberRole.Member,
                now);

        currentUser.UserId.Returns(memberId);
        clock.UtcNow.Returns(now);

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

        unitOfWork
            .SaveChangesAsync(
                Arg.Any<CancellationToken>())
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
            new CreateTaskCommand(
                ProjectId: project.Id,
                Title: "Member task",
                Description: null,
                Priority: TaskPriority.Medium,
                DueDateUtc: null);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        var result =
            await handler.HandleAsync(
                command,
                cancellationToken);

        Assert.Equal(
            project.Id,
            result.ProjectId);

        Assert.Equal(
            memberId,
            result.CreatedByUserId);

        Assert.Equal(
            TaskItemStatus.Backlog,
            result.Status);

        taskItemRepository
            .Received(1)
            .Add(
                Arg.Is<TaskItem>(
                    taskItem =>
                        taskItem.ProjectId == project.Id &&
                        taskItem.CreatedByUserId == memberId &&
                        taskItem.Title == "Member task"));

        await unitOfWork
            .Received(1)
            .SaveChangesAsync(
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

        var unitOfWork =
            Substitute.For<IUnitOfWork>();

        var currentUser =
            Substitute.For<ICurrentUser>();

        var clock =
            Substitute.For<IClock>();

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

        currentUser.UserId.Returns(memberId);

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
                unitOfWork,
                currentUser,
                clock);

        var command =
            CreateCommand(project.Id);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        var exception =
            await Assert.ThrowsAsync<ApplicationNotFoundException>(
                () => handler.HandleAsync(
                    command,
                    cancellationToken));

        Assert.Equal(
            "Project was not found.",
            exception.Message);

        taskItemRepository
            .DidNotReceive()
            .Add(
                Arg.Any<TaskItem>());

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

        currentUser.UserId.Returns(outsiderId);

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
                unitOfWork,
                currentUser,
                clock);

        var command =
            CreateCommand(project.Id);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        var exception =
            await Assert.ThrowsAsync<ApplicationNotFoundException>(
                () => handler.HandleAsync(
                    command,
                    cancellationToken));

        Assert.Equal(
            "Project was not found.",
            exception.Message);

        taskItemRepository
            .DidNotReceive()
            .Add(
                Arg.Any<TaskItem>());

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
                unitOfWork,
                currentUser,
                clock);

        var command =
            CreateCommand(projectId);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        var exception =
            await Assert.ThrowsAsync<ApplicationNotFoundException>(
                () => handler.HandleAsync(
                    command,
                    cancellationToken));

        Assert.Equal(
            "Project was not found.",
            exception.Message);

        taskItemRepository
            .DidNotReceive()
            .Add(
                Arg.Any<TaskItem>());

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

        project.Archive(
            now.AddMinutes(1));

        currentUser.UserId.Returns(ownerId);

        projectRepository
            .GetByIdAsync(
                project.Id,
                Arg.Any<CancellationToken>())
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
            CreateCommand(project.Id);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        var exception =
            await Assert.ThrowsAsync<ApplicationConflictException>(
                () => handler.HandleAsync(
                    command,
                    cancellationToken));

        Assert.Equal(
            "Cannot create tasks in an archived project.",
            exception.Message);

        taskItemRepository
            .DidNotReceive()
            .Add(
                Arg.Any<TaskItem>());

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
            CreateCommand(Guid.Empty);

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

        taskItemRepository
            .DidNotReceive()
            .Add(
                Arg.Any<TaskItem>());
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
            new CreateTaskCommand(
                ProjectId: Guid.NewGuid(),
                Title: "Invalid priority",
                Description: null,
                Priority: (TaskPriority)999,
                DueDateUtc: null);

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

        taskItemRepository
            .DidNotReceive()
            .Add(
                Arg.Any<TaskItem>());
    }

    private static CreateTaskHandler CreateHandler(
        IProjectRepository projectRepository,
        IProjectMemberRepository projectMemberRepository,
        ITaskItemRepository taskItemRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IClock clock)
    {
        return new CreateTaskHandler(
            projectRepository,
            projectMemberRepository,
            taskItemRepository,
            unitOfWork,
            currentUser,
            clock);
    }

    private static CreateTaskCommand CreateCommand(
        Guid projectId)
    {
        return new CreateTaskCommand(
            ProjectId: projectId,
            Title: "Test task",
            Description: "Test description",
            Priority: TaskPriority.Medium,
            DueDateUtc: null);
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