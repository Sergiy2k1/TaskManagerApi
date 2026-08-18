namespace TaskManager.Api.Contracts.Projects;

public sealed record CreateProjectResponse(
    Guid ProjectId,
    Guid OwnerId,
    string Name,
    string? Description,
    bool IsArchived,
    DateTimeOffset CreatedAtUtc);