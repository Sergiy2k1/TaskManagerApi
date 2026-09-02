using TaskManager.Domain.Enums;

namespace TaskManager.Api.Contracts.Tasks;

public sealed record UpdateTaskRequest(
    string Title,
    string? Description,
    TaskPriority Priority,
    DateTimeOffset? DueDateUtc);