using TaskManager.Application.Abstractions.Authentication;
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
    private readonly ICurrentUser _currentUser;

    public GetProjectMembersHandler(
        IProjectRepository projectRepository,
        IProjectMemberRepository projectMemberRepository,
        ICurrentUser currentUser)
    {
        _projectRepository = projectRepository;
        _projectMemberRepository = projectMemberRepository;
        _currentUser = currentUser;
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

        if (project.OwnerId != _currentUser.UserId)
        {
            var currentMember =
                await _projectMemberRepository
                    .GetByProjectAndUserAsync(
                        project.Id,
                        _currentUser.UserId,
                        cancellationToken);

            if (currentMember is null ||
                !currentMember.IsActive)
            {
                throw new ApplicationNotFoundException(
                    "Project was not found.");
            }
        }

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