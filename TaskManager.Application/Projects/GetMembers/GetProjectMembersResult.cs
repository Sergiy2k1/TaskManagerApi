using TaskManager.Domain.Enums;

namespace TaskManager.Application.Projects.GetMembers;

public sealed record GetProjectMembersResult(
    Guid ProjectMemberId,
    Guid UserId,
    ProjectMemberRole Role,
    DateTimeOffset JoinedAtUtc,
    DateTimeOffset? UpdatedAtUtc);