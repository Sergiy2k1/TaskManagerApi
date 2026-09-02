namespace TaskManager.Api.Contracts.Tasks;

public sealed record AssignTaskRequest(
    Guid AssigneeId);