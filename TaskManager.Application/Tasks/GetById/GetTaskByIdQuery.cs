using TaskManager.Application.Abstractions.Messaging;

namespace TaskManager.Application.Tasks.GetById;

public sealed record GetTaskByIdQuery(
    Guid ProjectId,
    Guid TaskItemId)
    : IQuery<GetTaskByIdResult>;