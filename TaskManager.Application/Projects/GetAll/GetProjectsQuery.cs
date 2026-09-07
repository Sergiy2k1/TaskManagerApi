using TaskManager.Application.Abstractions.Messaging;

namespace TaskManager.Application.Projects.GetAll;

public sealed record GetProjectsQuery
    : IQuery<IReadOnlyList<GetProjectsResult>>;
