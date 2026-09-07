namespace TaskManager.Application.Projects.Update;

public sealed record UpdateProjectResult(
    Guid ProjectId,
    Guid OwnerId,
    string Name,
    string? Description,
    bool IsArchived,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    DateTimeOffset? ArchivedAtUtc);
