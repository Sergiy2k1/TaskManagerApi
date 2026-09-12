using TaskManager.Application.Abstractions.Messaging;

namespace TaskManager.Application.TaskComments.GetAll;

public sealed record GetTaskCommentsQuery(
    Guid ProjectId,
    Guid TaskItemId)
    : IQuery<IReadOnlyList<GetTaskCommentsResult>>;
