namespace TaskManager.Api.Contracts.Projects;

public sealed record CreateProjectRequest(
    string Name,
    string? Description);