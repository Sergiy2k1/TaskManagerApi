using TaskManager.Application.Abstractions.Authentication;
using TaskManager.Application.Abstractions.Authorization;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Domain.Enums;

namespace TaskManager.Application.Common.Authorization;

public sealed class ProjectMemberManagementPolicy
    : IProjectMemberManagementPolicy
{
    private readonly IProjectMemberRepository _projectMemberRepository;
    private readonly ICurrentUser _currentUser;

    public ProjectMemberManagementPolicy(
        IProjectMemberRepository projectMemberRepository,
        ICurrentUser currentUser)
    {
        _projectMemberRepository = projectMemberRepository;
        _currentUser = currentUser;
    }

    public async Task EnsureCanManageMembersAsync(
        Guid projectOwnerId,
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        if (projectOwnerId == _currentUser.UserId)
        {
            return;
        }

        var currentMember =
            await _projectMemberRepository
                .GetByProjectAndUserAsync(
                    projectId,
                    _currentUser.UserId,
                    cancellationToken);

        if (currentMember is null ||
            !currentMember.IsActive)
        {
            throw new ApplicationNotFoundException(
                "Project was not found.");
        }

        if (currentMember.Role !=
            ProjectMemberRole.Manager)
        {
            throw new ApplicationForbiddenException(
                "You do not have permission to manage project members.");
        }
    }
}