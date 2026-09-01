using TaskManager.Domain.Enums;

namespace TaskManager.Api.Contracts.Projects;

public sealed record ChangeProjectMemberRoleRequest(
    ProjectMemberRole Role);