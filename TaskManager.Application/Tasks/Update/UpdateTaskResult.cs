using TaskManager.Domain.Enums;

namespace TaskManager.Application.Tasks.Update;

public sealed record UpdateTaskResult(
    Guid TaskItemId,
    Guid ProjectId,
    Guid CreatedByUserId,
    Guid? AssigneeId,
    string Title,
    string? Description,
    TaskItemStatus Status,
    TaskPriority Priority,
    DateTimeOffset? DueDateUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    DateTimeOffset? CompletedAtUtc);