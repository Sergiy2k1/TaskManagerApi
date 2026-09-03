using TaskManager.Domain.Enums;

namespace TaskManager.Api.Contracts.Tasks;

public sealed record ChangeTaskStatusResponse(
    Guid TaskItemId,
    Guid ProjectId,
    TaskItemStatus Status,
    DateTimeOffset? UpdatedAtUtc,
    DateTimeOffset? CompletedAtUtc);