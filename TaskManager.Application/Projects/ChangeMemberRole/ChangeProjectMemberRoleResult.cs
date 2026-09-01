using TaskManager.Domain.Enums;

namespace TaskManager.Application.Projects.ChangeMemberRole;

public sealed record ChangeProjectMemberRoleResult(
    Guid ProjectMemberId,
    Guid ProjectId,
    Guid UserId,
    ProjectMemberRole Role,
    DateTimeOffset? UpdatedAtUtc);