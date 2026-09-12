namespace TaskManager.Application.Projects.Restore;

public sealed record RestoreProjectResult(
    Guid ProjectId,
    Guid OwnerId,
    string Name,
    string? Description,
    bool IsArchived,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    DateTimeOffset? ArchivedAtUtc);
