using TaskManager.Domain.Enums;

namespace TaskManager.Application.Tasks.ChangeStatus;

public sealed record ChangeTaskStatusResult(
    Guid TaskItemId,
    Guid ProjectId,
    TaskItemStatus Status,
    DateTimeOffset? UpdatedAtUtc,
    DateTimeOffset? CompletedAtUtc);