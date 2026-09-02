using TaskManager.Application.Abstractions.Authentication;
using TaskManager.Application.Abstractions.Messaging;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Abstractions.Time;
using TaskManager.Application.Common.Exceptions;

namespace TaskManager.Application.Tasks.Assign;

public sealed class AssignTaskHandler
    : ICommandHandler<AssignTaskCommand, AssignTaskResult>
{
    private readonly IProjectRepository _projectRepository;
    private readonly IProjectMemberRepository _projectMemberRepository;
    private readonly ITaskItemRepository _taskItemRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public AssignTaskHandler(
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

    public async Task<AssignTaskResult> HandleAsync(
        AssignTaskCommand command,
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

        if (command.AssigneeId == Guid.Empty)
        {
            throw new ApplicationValidationException(
                "Assignee identifier cannot be empty.",
                nameof(command.AssigneeId));
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
                "Cannot assign tasks in an archived project.");
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

        var assigneeMember =
            await _projectMemberRepository
                .GetByProjectAndUserAsync(
                    project.Id,
                    command.AssigneeId,
                    cancellationToken);

        if (assigneeMember is null ||
            !assigneeMember.IsActive)
        {
            throw new ApplicationNotFoundException(
                "Assignee was not found in the project.");
        }

        taskItem.Assign(
            command.AssigneeId,
            _clock.UtcNow);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return new AssignTaskResult(
            TaskItemId: taskItem.Id,
            ProjectId: taskItem.ProjectId,
            AssigneeId: taskItem.AssigneeId!.Value,
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