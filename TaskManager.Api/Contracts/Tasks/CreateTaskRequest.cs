using TaskManager.Domain.Enums;

namespace TaskManager.Api.Contracts.Tasks;

public sealed record CreateTaskRequest(
    string Title,
    string? Description,
    TaskPriority Priority,
    DateTimeOffset? DueDateUtc);