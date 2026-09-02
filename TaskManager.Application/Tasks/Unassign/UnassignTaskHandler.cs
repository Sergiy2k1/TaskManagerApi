using TaskManager.Application.Abstractions.Authentication;
using TaskManager.Application.Abstractions.Messaging;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Abstractions.Time;
using TaskManager.Application.Common.Exceptions;

namespace TaskManager.Application.Tasks.Unassign;

public sealed class UnassignTaskHandler
    : ICommandHandler<UnassignTaskCommand, UnassignTaskResult>
{
    private readonly IProjectRepository _projectRepository;
    private readonly IProjectMemberRepository _projectMemberRepository;
    private readonly ITaskItemRepository _taskItemRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public UnassignTaskHandler(
        IProjectRepository projectRepository,
        IProjectMemberRepository projectMemberRepository,
        ITaskItemRepository taskItemRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IClock clock)
    {
        _projectRepository = projectRepository;
        _projectMemberRepository = projectMemberRepository;
        _taskItemRepository = taskItemRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<UnassignTaskResult> HandleAsync(
        UnassignTaskCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.ProjectId == Guid.Empty)
        {
            throw new ApplicationValidationException(
                "Project identifier cannot be empty.",
                nameof(command.ProjectId));
        }

        if (command.TaskItemId == Guid.Empty)
        {
            throw new ApplicationValidationException(
                "Task identifier cannot be empty.",
                nameof(command.TaskItemId));
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

        await EnsureCurrentUserHasProjectAccessAsync(
            project.OwnerId,
            project.Id,
            cancellationToken);

        if (project.IsArchived)
        {
            throw new ApplicationConflictException(
                "Cannot unassign tasks in an archived project.");
        }

        var taskItem =
            await _taskItemRepository.GetByIdAsync(
                command.TaskItemId,
                cancellationToken);

        if (taskItem is null ||
            taskItem.ProjectId != project.Id)
        {
            throw new ApplicationNotFoundException(
                "Task was not found.");
        }

        taskItem.Unassign(
            _clock.UtcNow);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return new UnassignTaskResult(
            TaskItemId: taskItem.Id,
            ProjectId: taskItem.ProjectId,
            UpdatedAtUtc: taskItem.UpdatedAtUtc);
    }

    private async Task EnsureCurrentUserHasProjectAccessAsync(
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
    }
}