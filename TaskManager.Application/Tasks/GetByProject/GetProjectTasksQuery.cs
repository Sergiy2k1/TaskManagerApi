using TaskManager.Application.Abstractions.Messaging;

namespace TaskManager.Application.Tasks.GetByProject;

public sealed record GetProjectTasksQuery(
    Guid ProjectId)
    : IQuery<IReadOnlyList<GetProjectTasksResult>>;