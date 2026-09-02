using TaskManager.Domain.Enums;

namespace TaskManager.Application.Tasks.Create;

public sealed record CreateTaskResult(
    Guid TaskItemId,
    Guid ProjectId,
    Guid CreatedByUserId,
    Guid? AssigneeId,
    string Title,
    string? Description,
    TaskItemStatus Status,
    TaskPriority Priority,
    DateTimeOffset? DueDateUtc,
    DateTimeOffset CreatedAtUtc);