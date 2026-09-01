using TaskManager.Domain.Enums;

namespace TaskManager.Api.Contracts.Projects;

public sealed record AddProjectMemberRequest(
    string Email,
    ProjectMemberRole Role);