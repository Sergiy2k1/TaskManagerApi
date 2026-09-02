using TaskManager.Application.Abstractions.Messaging;

namespace TaskManager.Application.Tasks.Assign;

public sealed record AssignTaskCommand(
    Guid ProjectId,
    Guid TaskItemId,
    Guid AssigneeId)
    : ICommand<AssignTaskResult>;