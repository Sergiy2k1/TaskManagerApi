using TaskManager.Application.Abstractions.Authorization;
using TaskManager.Application.Abstractions.Messaging;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Common.Exceptions;

namespace TaskManager.Application.Projects.GetMembers;

public sealed class GetProjectMembersHandler
    : IQueryHandler<
        GetProjectMembersQuery,
        IReadOnlyList<GetProjectMembersResult>>
{
    private readonly IProjectRepository _projectRepository;
    private readonly IProjectMemberRepository _projectMemberRepository;
    private readonly IProjectAccessPolicy _projectAccessPolicy;

    public GetProjectMembersHandler(
        IProjectRepository projectRepository,
        IProjectMemberRepository projectMemberRepository,
        IProjectAccessPolicy projectAccessPolicy)
    {
        _projectRepository = projectRepository;
        _projectMemberRepository = projectMemberRepository;
        _projectAccessPolicy = projectAccessPolicy;
    }

    public async Task<IReadOnlyList<GetProjectMembersResult>> HandleAsync(
        GetProjectMembersQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.ProjectId == Guid.Empty)
        {
            throw new ApplicationValidationException(
                "Project identifier cannot be empty.",
                nameof(query.ProjectId));
        }

        var project =
            await _projectRepository.GetByIdAsync(
                query.ProjectId,
                cancellationToken);

        if (project is null)
        {
            throw new ApplicationNotFoundException(
                "Project was not found.");
        }

        await _projectAccessPolicy.EnsureHasAccessAsync(
            project.OwnerId,
            project.Id,
            cancellationToken);

        var members =
            await _projectMemberRepository
                .GetActiveByProjectAsync(
                    project.Id,
                    cancellationToken);

        return members
            .Select(
                member =>
                    new GetProjectMembersResult(
                        ProjectMemberId: member.Id,
                        UserId: member.UserId,
                        Role: member.Role,
                        JoinedAtUtc: member.JoinedAtUtc,
                        UpdatedAtUtc: member.UpdatedAtUtc))
            .ToList();
    }
}