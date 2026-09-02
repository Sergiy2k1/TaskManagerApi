using TaskManager.Application.Abstractions.Messaging;

namespace TaskManager.Application.Projects.GetMembers;

public sealed record GetProjectMembersQuery(
    Guid ProjectId)
    : IQuery<IReadOnlyList<GetProjectMembersResult>>;