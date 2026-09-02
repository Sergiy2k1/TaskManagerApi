using TaskManager.Domain.Enums;

namespace TaskManager.Api.Contracts.Tasks;

public sealed record CreateTaskResponse(
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