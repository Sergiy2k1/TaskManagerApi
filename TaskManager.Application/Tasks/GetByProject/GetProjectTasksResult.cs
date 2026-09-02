using TaskManager.Domain.Enums;

namespace TaskManager.Application.Tasks.GetByProject;

public sealed record GetProjectTasksResult(
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