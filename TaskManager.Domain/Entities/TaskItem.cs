using TaskManager.Domain.Enums;
using TaskManager.Domain.Exceptions;

namespace TaskManager.Domain.Entities;

public sealed class TaskItem
{
    public const int MinTitleLength = 2;
    public const int MaxTitleLength = 200;
    public const int MaxDescriptionLength = 4000;

    public Guid Id { get; private set; }

    public Guid ProjectId { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public Guid? AssigneeId { get; private set; }

    public string Title { get; private set; } = null!;

    public string? Description { get; private set; }

    public TaskItemStatus Status { get; private set; }

    public TaskPriority Priority { get; private set; }

    public DateTimeOffset? DueDateUtc { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public DateTimeOffset? CompletedAtUtc { get; private set; }

    private TaskItem()
    {
    }

    public static TaskItem Create(
        Guid projectId,
        Guid createdByUserId,
        string title,
        string? description,
        TaskPriority priority,
        DateTimeOffset? dueDateUtc,
        DateTimeOffset createdAtUtc)
    {
        ValidateIdentifier(
            projectId,
            nameof(projectId),
            "Project identifier cannot be empty.");

        ValidateIdentifier(
            createdByUserId,
            nameof(createdByUserId),
            "Creator identifier cannot be empty.");

        var preparedTitle = PrepareTitle(title);
        var preparedDescription = PrepareDescription(description);

        ValidatePriority(priority);

        EnsureUtc(
            createdAtUtc,
            nameof(createdAtUtc));

        if (dueDateUtc.HasValue)
        {
            EnsureUtc(
                dueDateUtc.Value,
                nameof(dueDateUtc));

            if (dueDateUtc.Value < createdAtUtc)
            {
                throw new DomainValidationException(
                    "Due date cannot be earlier than task creation time.",
                    nameof(dueDateUtc));
            }
        }

        return new TaskItem
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            CreatedByUserId = createdByUserId,
            Title = preparedTitle,
            Description = preparedDescription,
            Status = TaskItemStatus.Backlog,
            Priority = priority,
            DueDateUtc = dueDateUtc,
            CreatedAtUtc = createdAtUtc
        };
    }

    public void Rename(
        string title,
        DateTimeOffset changedAtUtc)
    {
        EnsureCanBeModified();

        var preparedTitle = PrepareTitle(title);

        EnsureValidChangeTime(
            changedAtUtc,
            nameof(changedAtUtc));

        if (string.Equals(
                Title,
                preparedTitle,
                StringComparison.Ordinal))
        {
            return;
        }

        Title = preparedTitle;
        UpdatedAtUtc = changedAtUtc;
    }

    public void ChangeDescription(
        string? description,
        DateTimeOffset changedAtUtc)
    {
        EnsureCanBeModified();

        var preparedDescription =
            PrepareDescription(description);

        EnsureValidChangeTime(
            changedAtUtc,
            nameof(changedAtUtc));

        if (string.Equals(
                Description,
                preparedDescription,
                StringComparison.Ordinal))
        {
            return;
        }

        Description = preparedDescription;
        UpdatedAtUtc = changedAtUtc;
    }

    public void ChangePriority(
        TaskPriority priority,
        DateTimeOffset changedAtUtc)
    {
        EnsureCanBeModified();

        ValidatePriority(priority);

        EnsureValidChangeTime(
            changedAtUtc,
            nameof(changedAtUtc));

        if (Priority == priority)
        {
            return;
        }

        Priority = priority;
        UpdatedAtUtc = changedAtUtc;
    }

    public void Assign(
        Guid userId,
        DateTimeOffset changedAtUtc)
    {
        EnsureCanBeModified();

        ValidateIdentifier(
            userId,
            nameof(userId),
            "Assignee identifier cannot be empty.");

        EnsureValidChangeTime(
            changedAtUtc,
            nameof(changedAtUtc));

        if (AssigneeId == userId)
        {
            return;
        }

        AssigneeId = userId;
        UpdatedAtUtc = changedAtUtc;
    }

    public void Unassign(DateTimeOffset changedAtUtc)
    {
        EnsureCanBeModified();

        EnsureValidChangeTime(
            changedAtUtc,
            nameof(changedAtUtc));

        if (AssigneeId is null)
        {
            return;
        }

        AssigneeId = null;
        UpdatedAtUtc = changedAtUtc;
    }

    public void ChangeDueDate(
        DateTimeOffset? dueDateUtc,
        DateTimeOffset changedAtUtc)
    {
        EnsureCanBeModified();

        EnsureValidChangeTime(
            changedAtUtc,
            nameof(changedAtUtc));

        if (dueDateUtc.HasValue)
        {
            EnsureUtc(
                dueDateUtc.Value,
                nameof(dueDateUtc));

            if (dueDateUtc.Value < CreatedAtUtc)
            {
                throw new DomainValidationException(
                    "Due date cannot be earlier than task creation time.",
                    nameof(dueDateUtc));
            }
        }

        if (DueDateUtc == dueDateUtc)
        {
            return;
        }

        DueDateUtc = dueDateUtc;
        UpdatedAtUtc = changedAtUtc;
    }

    public void ChangeStatus(
        TaskItemStatus status,
        DateTimeOffset changedAtUtc)
    {
        ValidateStatus(status);

        EnsureValidChangeTime(
            changedAtUtc,
            nameof(changedAtUtc));

        if (Status == status)
        {
            return;
        }

        EnsureValidStatusTransition(
            Status,
            status);

        Status = status;
        UpdatedAtUtc = changedAtUtc;

        if (status == TaskItemStatus.Completed)
        {
            CompletedAtUtc = changedAtUtc;
        }
        else
        {
            CompletedAtUtc = null;
        }
    }

    private static void EnsureValidStatusTransition(
        TaskItemStatus currentStatus,
        TaskItemStatus newStatus)
    {
        var isValid = currentStatus switch
        {
            TaskItemStatus.Backlog =>
                newStatus is TaskItemStatus.Todo
                    or TaskItemStatus.Cancelled,

            TaskItemStatus.Todo =>
                newStatus is TaskItemStatus.Backlog
                    or TaskItemStatus.InProgress
                    or TaskItemStatus.Cancelled,

            TaskItemStatus.InProgress =>
                newStatus is TaskItemStatus.Todo
                    or TaskItemStatus.Review
                    or TaskItemStatus.Cancelled,

            TaskItemStatus.Review =>
                newStatus is TaskItemStatus.InProgress
                    or TaskItemStatus.Completed
                    or TaskItemStatus.Cancelled,

            TaskItemStatus.Completed =>
                newStatus is TaskItemStatus.Review,

            TaskItemStatus.Cancelled =>
                newStatus is TaskItemStatus.Backlog,

            _ => false
        };

        if (!isValid)
        {
            throw new DomainConflictException(
                $"Cannot change task status from {currentStatus} to {newStatus}.");
        }
    }

    private void EnsureCanBeModified()
    {
        if (Status is TaskItemStatus.Completed
            or TaskItemStatus.Cancelled)
        {
            throw new DomainConflictException(
                "Completed or cancelled task cannot be modified.");
        }
    }

    private static string PrepareTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainValidationException(
                "Task title cannot be empty.",
                nameof(title));
        }

        var preparedTitle = title.Trim();

        if (preparedTitle.Length < MinTitleLength)
        {
            throw new DomainValidationException(
                $"Task title must contain at least {MinTitleLength} characters.",
                nameof(title));
        }

        if (preparedTitle.Length > MaxTitleLength)
        {
            throw new DomainValidationException(
                $"Task title cannot exceed {MaxTitleLength} characters.",
                nameof(title));
        }

        return preparedTitle;
    }

    private static string? PrepareDescription(
        string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var preparedDescription = description.Trim();

        if (preparedDescription.Length > MaxDescriptionLength)
        {
            throw new DomainValidationException(
                $"Task description cannot exceed {MaxDescriptionLength} characters.",
                nameof(description));
        }

        return preparedDescription;
    }

    private static void ValidatePriority(TaskPriority priority)
    {
        if (!Enum.IsDefined(priority))
        {
            throw new DomainValidationException(
                $"Unsupported task priority: {priority}.",
                nameof(priority));
        }
    }

    private static void ValidateStatus(TaskItemStatus status)
    {
        if (!Enum.IsDefined(status))
        {
            throw new DomainValidationException(
                $"Unsupported task status: {status}.",
                nameof(status));
        }
    }

    private static void ValidateIdentifier(
        Guid identifier,
        string parameterName,
        string errorMessage)
    {
        if (identifier == Guid.Empty)
        {
            throw new DomainValidationException(
                errorMessage,
                parameterName);
        }
    }

    private void EnsureValidChangeTime(
        DateTimeOffset changedAtUtc,
        string parameterName)
    {
        EnsureUtc(
            changedAtUtc,
            parameterName);

        if (changedAtUtc < CreatedAtUtc)
        {
            throw new DomainValidationException(
                "Change time cannot be earlier than task creation time.",
                parameterName);
        }

        if (UpdatedAtUtc.HasValue &&
            changedAtUtc < UpdatedAtUtc.Value)
        {
            throw new DomainValidationException(
                "Change time cannot be earlier than the previous change time.",
                parameterName);
        }
    }

    private static void EnsureUtc(
        DateTimeOffset value,
        string parameterName)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainValidationException(
                "Date and time must be in UTC.",
                parameterName);
        }
    }
}