using TaskManager.Application.Abstractions.Messaging;

namespace TaskManager.Application.Projects.GetById;

public sealed record GetProjectByIdQuery(
    Guid ProjectId)
    : IQuery<GetProjectByIdResult>;