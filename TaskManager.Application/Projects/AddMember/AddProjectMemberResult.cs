using TaskManager.Domain.Enums;

namespace TaskManager.Application.Projects.AddMember;

public sealed record AddProjectMemberResult(
    Guid ProjectMemberId,
    Guid ProjectId,
    Guid UserId,
    ProjectMemberRole Role,
    DateTimeOffset JoinedAtUtc);