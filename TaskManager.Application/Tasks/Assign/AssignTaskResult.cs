namespace TaskManager.Application.Tasks.Assign;

public sealed record AssignTaskResult(
    Guid TaskItemId,
    Guid ProjectId,
    Guid AssigneeId,
    DateTimeOffset? UpdatedAtUtc);