namespace TaskManager.Application.Projects.Create;

public sealed record CreateProjectCommand(
    string Name,
    string? Description);