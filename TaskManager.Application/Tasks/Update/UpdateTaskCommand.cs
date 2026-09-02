using TaskManager.Application.Abstractions.Messaging;
using TaskManager.Domain.Enums;

namespace TaskManager.Application.Tasks.Update;

public sealed record UpdateTaskCommand(
    Guid ProjectId,
    Guid TaskItemId,
    string Title,
    string? Description,
    TaskPriority Priority,
    DateTimeOffset? DueDateUtc)
    : ICommand<UpdateTaskResult>;