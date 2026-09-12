using NSubstitute;
using TaskManager.Application.Abstractions.Authentication;
using TaskManager.Application.Abstractions.Authorization;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Abstractions.Time;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.TaskComments.Add;
using TaskManager.Application.TaskComments.Delete;
using TaskManager.Application.TaskComments.Edit;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using Xunit;

namespace TaskManager.Application.UnitTests.TaskComments;

public sealed class TaskCommentHandlerTests
{
    [Fact]
    public async Task AddCreatesCommentForAccessibleTask()
    {
        var projectRepo = Substitute.For<IProjectRepository>();
        var taskRepo = Substitute.For<ITaskItemRepository>();
        var commentRepo = Substitute.For<ITaskCommentRepository>();
        var policy = Substitute.For<IProjectAccessPolicy>();
        var uow = Substitute.For<IUnitOfWork>();
        var currentUser = Substitute.For<ICurrentUser>();
        var clock = Substitute.For<IClock>();
        var now = UtcTime();
        var userId = Guid.NewGuid();
        var project = Project.Create(userId, "Comments", null, now);
        var task = TaskItem.Create(project.Id, userId, "Task", null, TaskPriority.Medium, null, now);
        var token = TestContext.Current.CancellationToken;

        projectRepo.GetByIdAsync(project.Id, token).Returns(project);
        taskRepo.GetByIdAsync(task.Id, token).Returns(task);
        currentUser.UserId.Returns(userId);
        clock.UtcNow.Returns(now.AddMinutes(1));
        uow.SaveChangesAsync(token).Returns(1);

        var handler = new AddTaskCommentHandler(
            projectRepo, taskRepo, commentRepo, policy, uow, currentUser, clock);

        var result = await handler.HandleAsync(
            new AddTaskCommentCommand(project.Id, task.Id, " Hello "),
            token);

        Assert.Equal("Hello", result.Content);
        Assert.Equal(userId, result.AuthorUserId);
        commentRepo.Received(1).Add(Arg.Is<TaskComment>(
            comment => comment.TaskItemId == task.Id && comment.Content == "Hello"));
        await uow.Received(1).SaveChangesAsync(token);
    }

    [Fact]
    public async Task EditByDifferentUserThrowsForbidden()
    {
        var projectRepo = Substitute.For<IProjectRepository>();
        var taskRepo = Substitute.For<ITaskItemRepository>();
        var commentRepo = Substitute.For<ITaskCommentRepository>();
        var policy = Substitute.For<IProjectAccessPolicy>();
        var uow = Substitute.For<IUnitOfWork>();
        var currentUser = Substitute.For<ICurrentUser>();
        var clock = Substitute.For<IClock>();
        var now = UtcTime();
        var ownerId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var project = Project.Create(ownerId, "Comments", null, now);
        var task = TaskItem.Create(project.Id, ownerId, "Task", null, TaskPriority.Medium, null, now);
        var comment = TaskComment.Create(task.Id, authorId, "Original", now);
        var token = TestContext.Current.CancellationToken;

        projectRepo.GetByIdAsync(project.Id, token).Returns(project);
        taskRepo.GetByIdAsync(task.Id, token).Returns(task);
        commentRepo.GetByIdForUpdateAsync(comment.Id, token).Returns(comment);
        currentUser.UserId.Returns(ownerId);

        var handler = new EditTaskCommentHandler(
            projectRepo, taskRepo, commentRepo, policy, uow, currentUser, clock);

        await Assert.ThrowsAsync<ApplicationForbiddenException>(
            () => handler.HandleAsync(
                new EditTaskCommentCommand(project.Id, task.Id, comment.Id, "Changed"),
                token));

        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProjectOwnerCanDeleteAnotherUsersComment()
    {
        var projectRepo = Substitute.For<IProjectRepository>();
        var taskRepo = Substitute.For<ITaskItemRepository>();
        var commentRepo = Substitute.For<ITaskCommentRepository>();
        var policy = Substitute.For<IProjectAccessPolicy>();
        var uow = Substitute.For<IUnitOfWork>();
        var currentUser = Substitute.For<ICurrentUser>();
        var clock = Substitute.For<IClock>();
        var now = UtcTime();
        var ownerId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var project = Project.Create(ownerId, "Comments", null, now);
        var task = TaskItem.Create(project.Id, ownerId, "Task", null, TaskPriority.Medium, null, now);
        var comment = TaskComment.Create(task.Id, authorId, "Delete me", now);
        var deletedAt = now.AddMinutes(2);
        var token = TestContext.Current.CancellationToken;

        projectRepo.GetByIdAsync(project.Id, token).Returns(project);
        taskRepo.GetByIdAsync(task.Id, token).Returns(task);
        commentRepo.GetByIdForUpdateAsync(comment.Id, token).Returns(comment);
        currentUser.UserId.Returns(ownerId);
        clock.UtcNow.Returns(deletedAt);
        uow.SaveChangesAsync(token).Returns(1);

        var handler = new DeleteTaskCommentHandler(
            projectRepo, taskRepo, commentRepo, policy, uow, currentUser, clock);

        var result = await handler.HandleAsync(
            new DeleteTaskCommentCommand(project.Id, task.Id, comment.Id),
            token);

        Assert.Equal(deletedAt, result.DeletedAtUtc);
        await uow.Received(1).SaveChangesAsync(token);
    }

    private static DateTimeOffset UtcTime() =>
        new(2026, 9, 12, 17, 0, 0, TimeSpan.Zero);
}
