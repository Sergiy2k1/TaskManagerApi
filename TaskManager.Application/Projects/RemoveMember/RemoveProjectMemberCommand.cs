using TaskManager.Application.Abstractions.Messaging;

namespace TaskManager.Application.Projects.RemoveMember;

public sealed record RemoveProjectMemberCommand(
    Guid ProjectId,
    Guid UserId)
    : ICommand<RemoveProjectMemberResult>;