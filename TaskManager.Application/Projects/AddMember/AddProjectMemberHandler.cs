using TaskManager.Application.Abstractions.Authentication;
using TaskManager.Application.Abstractions.Messaging;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Abstractions.Time;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;

namespace TaskManager.Application.Projects.AddMember;

public sealed class AddProjectMemberHandler
    : ICommandHandler<AddProjectMemberCommand, AddProjectMemberResult>
{
    private readonly IProjectRepository _projectRepository;
    private readonly IProjectMemberRepository _projectMemberRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public AddProjectMemberHandler(
        IProjectRepository projectRepository,
        IProjectMemberRepository projectMemberRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IClock clock)
    {
        _projectRepository = projectRepository;
        _projectMemberRepository = projectMemberRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<AddProjectMemberResult> HandleAsync(
        AddProjectMemberCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.ProjectId == Guid.Empty)
        {
            throw new ApplicationValidationException(
                "Project identifier cannot be empty.",
                nameof(command.ProjectId));
        }

        if (!Enum.IsDefined(command.Role))
        {
            throw new ApplicationValidationException(
                $"Unsupported project member role: {command.Role}.",
                nameof(command.Role));
        }

        var normalizedEmail =
            User.NormalizeEmail(command.Email);

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

        var user =
            await _userRepository.GetByNormalizedEmailAsync(
                normalizedEmail,
                cancellationToken);

        if (user is null ||
            !user.IsActive)
        {
            throw new ApplicationNotFoundException(
                "User was not found.");
        }

        var existingMember =
            await _projectMemberRepository
                .GetByProjectAndUserAsync(
                    project.Id,
                    user.Id,
                    cancellationToken);

        var now =
            _clock.UtcNow;

        ProjectMember projectMember;

        if (existingMember is null)
        {
            projectMember = ProjectMember.Create(
                projectId: project.Id,
                userId: user.Id,
                role: command.Role,
                joinedAtUtc: now);

            _projectMemberRepository.Add(
                projectMember);
        }
        else
        {
            if (existingMember.IsActive)
            {
                throw new ApplicationConflictException(
                    "User is already an active project member.");
            }

            existingMember.Restore(now);
            existingMember.ChangeRole(
                command.Role,
                now);

            projectMember =
                existingMember;
        }

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return new AddProjectMemberResult(
            ProjectMemberId: projectMember.Id,
            ProjectId: projectMember.ProjectId,
            UserId: projectMember.UserId,
            Role: projectMember.Role,
            JoinedAtUtc: projectMember.JoinedAtUtc);
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