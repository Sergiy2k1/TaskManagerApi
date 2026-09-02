using TaskManager.Application.Abstractions.Authorization;
using TaskManager.Application.Abstractions.Messaging;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Abstractions.Time;
using TaskManager.Application.Common.Exceptions;


namespace TaskManager.Application.Projects.ChangeMemberRole;

public sealed class ChangeProjectMemberRoleHandler
    : ICommandHandler<
        ChangeProjectMemberRoleCommand,
        ChangeProjectMemberRoleResult>
{
    private readonly IProjectRepository _projectRepository;
    private readonly IProjectMemberRepository _projectMemberRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProjectMemberManagementPolicy _memberManagementPolicy;
    private readonly IClock _clock;

    public ChangeProjectMemberRoleHandler(
        IProjectRepository projectRepository,
        IProjectMemberRepository projectMemberRepository,
        IUnitOfWork unitOfWork,
        IProjectMemberManagementPolicy memberManagementPolicy,
        IClock clock)
    {
        _projectRepository = projectRepository;
        _projectMemberRepository = projectMemberRepository;
        _unitOfWork = unitOfWork;
        _memberManagementPolicy = memberManagementPolicy;
        _clock = clock;
    }

    public async Task<ChangeProjectMemberRoleResult> HandleAsync(
        ChangeProjectMemberRoleCommand command,
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

        if (!Enum.IsDefined(command.Role))
        {
            throw new ApplicationValidationException(
                $"Unsupported project member role: {command.Role}.",
                nameof(command.Role));
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

        await _memberManagementPolicy
            .EnsureCanManageMembersAsync(
                project.OwnerId,
                project.Id,
                cancellationToken);

        if (command.UserId == project.OwnerId)
        {
            throw new ApplicationConflictException(
                "Project owner role cannot be changed.");
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

        targetMember.ChangeRole(
            command.Role,
            _clock.UtcNow);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return new ChangeProjectMemberRoleResult(
            ProjectMemberId: targetMember.Id,
            ProjectId: targetMember.ProjectId,
            UserId: targetMember.UserId,
            Role: targetMember.Role,
            UpdatedAtUtc: targetMember.UpdatedAtUtc);
    }
}