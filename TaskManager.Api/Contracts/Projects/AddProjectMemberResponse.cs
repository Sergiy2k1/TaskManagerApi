using TaskManager.Domain.Enums;

namespace TaskManager.Api.Contracts.Projects;

public sealed record AddProjectMemberResponse(
    Guid ProjectMemberId,
    Guid ProjectId,
    Guid UserId,
    ProjectMemberRole Role,
    DateTimeOffset JoinedAtUtc);