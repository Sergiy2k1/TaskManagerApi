namespace TaskManager.Application.Projects.RemoveMember;

public sealed record RemoveProjectMemberResult(
    Guid ProjectMemberId,
    Guid ProjectId,
    Guid UserId,
    DateTimeOffset RemovedAtUtc);