using TaskManager.Application.Abstractions.Messaging;
using TaskManager.Domain.Enums;

namespace TaskManager.Application.Tasks.ChangeStatus;

public sealed record ChangeTaskStatusCommand(
    Guid ProjectId,
    Guid TaskItemId,
    TaskItemStatus Status)
    : ICommand<ChangeTaskStatusResult>;