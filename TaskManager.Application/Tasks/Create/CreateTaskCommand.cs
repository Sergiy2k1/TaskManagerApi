using TaskManager.Application.Abstractions.Messaging;
using TaskManager.Domain.Enums;

namespace TaskManager.Application.Tasks.Create;

public sealed record CreateTaskCommand(
    Guid ProjectId,
    string Title,
    string? Description,
    TaskPriority Priority,
    DateTimeOffset? DueDateUtc)
    : ICommand<CreateTaskResult>;