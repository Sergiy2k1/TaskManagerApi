using TaskManager.Domain.Enums;

namespace TaskManager.Api.Contracts.Tasks;

public sealed record ChangeTaskStatusRequest(
    TaskItemStatus Status);