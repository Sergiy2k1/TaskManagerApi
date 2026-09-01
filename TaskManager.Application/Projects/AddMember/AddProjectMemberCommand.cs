using TaskManager.Application.Abstractions.Messaging;
using TaskManager.Domain.Enums;

namespace TaskManager.Application.Projects.AddMember;

public sealed record AddProjectMemberCommand(
    Guid ProjectId,
    string Email,
    ProjectMemberRole Role)
    : ICommand<AddProjectMemberResult>;