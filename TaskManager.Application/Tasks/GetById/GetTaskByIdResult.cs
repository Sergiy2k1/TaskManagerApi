using TaskManager.Domain.Enums;

namespace TaskManager.Application.Tasks.GetById;

public sealed record GetTaskByIdResult(
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