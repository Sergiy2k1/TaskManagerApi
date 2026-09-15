using NSubstitute;
using TaskManager.Application.Abstractions.Authentication;
using TaskManager.Application.Abstractions.Authorization;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Abstractions.Time;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Projects.Update;
using TaskManager.Application.Tasks.Update;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using Xunit;

namespace TaskManager.Application.UnitTests.Concurrency;

public sealed class OptimisticConcurrencyHandlerTests
{
    [Fact]
    public async Task UpdateProjectWithStaleVersionThrowsConflict()
    {
        var repository = Substitute.For<IProjectRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var policy = Substitute.For<IProjectAccessPolicy>();
        var currentUser = Substitute.For<ICurrentUser>();
        var clock = Substitute.For<IClock>();

        var ownerId = Guid.NewGuid();
        var project = Project.Create(
            ownerId,
            "Concurrency Project",
            null,
            UtcNow());

        currentUser.UserId.Returns(ownerId);
        var token = TestContext.Current.CancellationToken;

        repository.GetByIdForUpdateAsync(project.Id, token)
            .Returns(project);

        var handler = new UpdateProjectHandler(
            repository,
            unitOfWork,
            policy,
            currentUser,
            clock);

        var exception =
            await Assert.ThrowsAsync<ApplicationConflictException>(
                () => handler.HandleAsync(
                    new UpdateProjectCommand(
                        project.Id,
                        "Changed Project",
                        null,
                        ExpectedVersion: 2),
                    token));

        Assert.Equal(
            "Project was modified since it was loaded. Reload the project and retry.",
            exception.Message);

        await unitOfWork.DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateTaskWithStaleVersionThrowsConflict()
    {
        var projectRepository = Substitute.For<IProjectRepository>();
        var taskRepository = Substitute.For<ITaskItemRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var policy = Substitute.For<IProjectAccessPolicy>();
        var clock = Substitute.For<IClock>();

        var ownerId = Guid.NewGuid();
        var project = Project.Create(
            ownerId,
            "Concurrency Project",
            null,
            UtcNow());

        var task = TaskItem.Create(
            project.Id,
            ownerId,
            "Concurrency Task",
            null,
            TaskPriority.Medium,
            null,
            UtcNow());

        var token = TestContext.Current.CancellationToken;

        projectRepository.GetByIdAsync(project.Id, token)
            .Returns(project);

        taskRepository.GetByIdAsync(task.Id, token)
            .Returns(task);

        var handler = new UpdateTaskHandler(
            projectRepository,
            taskRepository,
            unitOfWork,
            policy,
            clock);

        var exception =
            await Assert.ThrowsAsync<ApplicationConflictException>(
                () => handler.HandleAsync(
                    new UpdateTaskCommand(
                        project.Id,
                        task.Id,
                        "Changed Task",
                        null,
                        TaskPriority.High,
                        null,
                        ExpectedVersion: 2),
                    token));

        Assert.Equal(
            "Task was modified since it was loaded. Reload the task and retry.",
            exception.Message);

        await unitOfWork.DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static DateTimeOffset UtcNow() =>
        new(
            2026,
            9,
            15,
            20,
            30,
            0,
            TimeSpan.Zero);
}
