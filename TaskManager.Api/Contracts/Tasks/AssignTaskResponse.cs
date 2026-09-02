namespace TaskManager.Api.Contracts.Tasks;

public sealed record AssignTaskResponse(
    Guid TaskItemId,
    Guid ProjectId,
    Guid AssigneeId,
    DateTimeOffset? UpdatedAtUtc);