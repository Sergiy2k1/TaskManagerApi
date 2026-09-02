using TaskManager.Application.Abstractions.Messaging;

namespace TaskManager.Application.Tasks.Unassign;

public sealed record UnassignTaskCommand(
    Guid ProjectId,
    Guid TaskItemId)
    : ICommand<UnassignTaskResult>;