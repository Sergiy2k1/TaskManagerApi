using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using TaskManager.Domain.Exceptions;
using Xunit;

namespace TaskManager.Domain.UnitTests.Entities;

public sealed class TaskItemTests
{
    private static readonly DateTimeOffset CreatedAtUtc =
        new(2026, 8, 29, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ChangeStatusWhenTransitionIsValidUpdatesStatusAndCompletionTime()
    {
        var task = CreateTask();

        task.ChangeStatus(
            TaskItemStatus.Todo,
            CreatedAtUtc.AddMinutes(1));

        task.ChangeStatus(
            TaskItemStatus.InProgress,
            CreatedAtUtc.AddMinutes(2));

        task.ChangeStatus(
            TaskItemStatus.Review,
            CreatedAtUtc.AddMinutes(3));

        var completedAtUtc =
            CreatedAtUtc.AddMinutes(4);

        task.ChangeStatus(
            TaskItemStatus.Completed,
            completedAtUtc);

        Assert.Equal(
            TaskItemStatus.Completed,
            task.Status);

        Assert.Equal(
            completedAtUtc,
            task.CompletedAtUtc);

        Assert.Equal(
            completedAtUtc,
            task.UpdatedAtUtc);
    }

    [Fact]
    public void ChangeStatusFromBacklogDirectlyToCompletedThrowsDomainConflictException()
    {
        var task = CreateTask();

        var exception =
            Assert.Throws<DomainConflictException>(() =>
                task.ChangeStatus(
                    TaskItemStatus.Completed,
                    CreatedAtUtc.AddMinutes(1)));

        Assert.Equal(
            "Cannot change task status from Backlog to Completed.",
            exception.Message);
    }

    [Fact]
    public void RenameWhenTaskIsCompletedThrowsDomainConflictException()
    {
        var task = CreateTask();

        task.ChangeStatus(
            TaskItemStatus.Todo,
            CreatedAtUtc.AddMinutes(1));

        task.ChangeStatus(
            TaskItemStatus.InProgress,
            CreatedAtUtc.AddMinutes(2));

        task.ChangeStatus(
            TaskItemStatus.Review,
            CreatedAtUtc.AddMinutes(3));

        task.ChangeStatus(
            TaskItemStatus.Completed,
            CreatedAtUtc.AddMinutes(4));

        var exception =
            Assert.Throws<DomainConflictException>(() =>
                task.Rename(
                    "Updated title",
                    CreatedAtUtc.AddMinutes(5)));

        Assert.Equal(
            "Completed or cancelled task cannot be modified.",
            exception.Message);
    }

    private static TaskItem CreateTask()
    {
        return TaskItem.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Implement tests",
            null,
            TaskPriority.Medium,
            null,
            CreatedAtUtc);
    }
}