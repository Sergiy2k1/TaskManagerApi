using TaskManager.Application.Abstractions.Messaging;
using TaskManager.Domain.Enums;

namespace TaskManager.Application.Projects.ChangeMemberRole;

public sealed record ChangeProjectMemberRoleCommand(
    Guid ProjectId,
    Guid UserId,
    ProjectMemberRole Role)
    : ICommand<ChangeProjectMemberRoleResult>;