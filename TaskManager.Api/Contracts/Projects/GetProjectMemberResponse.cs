using TaskManager.Domain.Enums;

namespace TaskManager.Api.Contracts.Projects;

public sealed record GetProjectMemberResponse(
    Guid ProjectMemberId,
    Guid UserId,
    ProjectMemberRole Role,
    DateTimeOffset JoinedAtUtc,
    DateTimeOffset? UpdatedAtUtc);