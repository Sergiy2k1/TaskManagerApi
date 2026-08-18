namespace TaskManager.Application.Projects.Create;

public sealed record CreateProjectResult(
    Guid ProjectId,
    Guid OwnerId,
    string Name,
    string? Description,
    bool IsArchived,
    DateTimeOffset CreatedAtUtc);