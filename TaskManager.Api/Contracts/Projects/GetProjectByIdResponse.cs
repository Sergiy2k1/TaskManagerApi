namespace TaskManager.Api.Contracts.Projects;

public sealed record GetProjectByIdResponse(
    Guid ProjectId,
    Guid OwnerId,
    string Name,
    string? Description,
    bool IsArchived,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    DateTimeOffset? ArchivedAtUtc);