using TaskManager.Application.Abstractions.Messaging;

namespace TaskManager.Application.Projects.Archive;

public sealed record ArchiveProjectCommand(
    Guid ProjectId)
    : ICommand<ArchiveProjectResult>;
