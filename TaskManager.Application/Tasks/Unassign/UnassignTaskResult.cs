namespace TaskManager.Application.Tasks.Unassign;

public sealed record UnassignTaskResult(
    Guid TaskItemId,
    Guid ProjectId,
    DateTimeOffset? UpdatedAtUtc);