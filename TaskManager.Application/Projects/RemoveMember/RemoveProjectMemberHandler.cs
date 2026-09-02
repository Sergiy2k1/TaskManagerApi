using TaskManager.Application.Abstractions.Authorization;
using TaskManager.Application.Abstractions.Messaging;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Abstractions.Time;
using TaskManager.Application.Common.Exceptions;

namespace TaskManager.Application.Projects.RemoveMember;

public sealed class RemoveProjectMemberHandler
    : ICommandHandler<
        RemoveProjectMemberCommand,
        RemoveProjectMemberResult>
{
    private readonly IProjectRepository _projectRepository;
    private readonly IProjectMemberRepository _projectMemberRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProjectMemberManagementPolicy _memberManagementPolicy;
    private readonly IClock _clock;

    public RemoveProjectMemberHandler(
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

        await _memberManagementPolicy
            .EnsureCanManageMembersAsync(
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
}