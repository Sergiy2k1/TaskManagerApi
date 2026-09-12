using TaskManager.Application.Abstractions.Messaging;

namespace TaskManager.Application.Projects.Restore;

public sealed record RestoreProjectCommand(
    Guid ProjectId)
    : ICommand<RestoreProjectResult>;
