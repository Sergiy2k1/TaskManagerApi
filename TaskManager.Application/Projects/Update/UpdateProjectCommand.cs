using TaskManager.Application.Abstractions.Messaging;

namespace TaskManager.Application.Projects.Update;

public sealed record UpdateProjectCommand(
    Guid ProjectId,
    string Name,
    string? Description,
    long ExpectedVersion = 1)
    : ICommand<UpdateProjectResult>;
