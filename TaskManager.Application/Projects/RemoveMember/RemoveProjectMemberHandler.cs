using TaskManager.Application.Abstractions.Authentication;
using TaskManager.Application.Abstractions.Messaging;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Abstractions.Time;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Domain.Enums;

namespace TaskManager.Application.Projects.RemoveMember;

public sealed class RemoveProjectMemberHandler
    : ICommandHandler<
        RemoveProjectMemberCommand,
        RemoveProjectMemberResult>
{
    private readonly IProjectRepository _projectRepository;
    private readonly IProjectMemberRepository _projectMemberRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public RemoveProjectMemberHandler(
        IProjectRepository projectRepository,
        IProjectMemberRepository projectMemberRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IClock clock)
    {
        _projectRepository = projectRepository;
        _projectMemberRepository = projectMemberRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<RemoveProjectMemberResult> HandleAsync(
        RemoveProjectMemberCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.ProjectId == Guid.Empty)
        {
            throw new ApplicationValidationException(
                "Project identifier cannot be empty.",
                nameof(command.ProjectId));
        }

        if (command.UserId == Guid.Empty)
        {
            throw new ApplicationValidationException(
                "User identifier cannot be empty.",
                nameof(command.UserId));
        }

        var project =
            await _projectRepository.GetByIdAsync(
                command.ProjectId,
                cancellationToken);

        if (project is null)
        {
            throw new ApplicationNotFoundException(
                "Project was not found.");
        }

        await EnsureCurrentUserCanManageMembersAsync(
            project.OwnerId,
            project.Id,
            cancellationToken);

        if (command.UserId == project.OwnerId)
        {
            throw new ApplicationConflictException(
                "Project owner cannot be removed.");
        }

        var targetMember =
            await _projectMemberRepository
                .GetByProjectAndUserAsync(
                    project.Id,
                    command.UserId,
                    cancellationToken);

        if (targetMember is null ||
            !targetMember.IsActive)
        {
            throw new ApplicationNotFoundException(
                "Project member was not found.");
        }

        var removedAtUtc =
            _clock.UtcNow;

        targetMember.Remove(
            removedAtUtc);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return new RemoveProjectMemberResult(
            ProjectMemberId: targetMember.Id,
            ProjectId: targetMember.ProjectId,
            UserId: targetMember.UserId,
            RemovedAtUtc: removedAtUtc);
    }

    private async Task EnsureCurrentUserCanManageMembersAsync(
        Guid ownerId,
        Guid projectId,
        CancellationToken cancellationToken)
    {
        if (ownerId == _currentUser.UserId)
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