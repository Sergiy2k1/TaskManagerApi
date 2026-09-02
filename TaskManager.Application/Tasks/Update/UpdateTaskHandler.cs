using TaskManager.Application.Abstractions.Authentication;
using TaskManager.Application.Abstractions.Messaging;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Abstractions.Time;
using TaskManager.Application.Common.Exceptions;

namespace TaskManager.Application.Tasks.Update;

public sealed class UpdateTaskHandler
    : ICommandHandler<UpdateTaskCommand, UpdateTaskResult>
{
    private readonly IProjectRepository _projectRepository;
    private readonly IProjectMemberRepository _projectMemberRepository;
    private readonly ITaskItemRepository _taskItemRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public UpdateTaskHandler(
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

    public async Task<UpdateTaskResult> HandleAsync(
        UpdateTaskCommand command,
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

        if (!Enum.IsDefined(command.Priority))
        {
            throw new ApplicationValidationException(
                $"Unsupported task priority: {command.Priority}.",
                nameof(command.Priority));
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
                "Cannot update tasks in an archived project.");
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

        var changedAtUtc =
            _clock.UtcNow;

        taskItem.Rename(
            command.Title,
            changedAtUtc);

        taskItem.ChangeDescription(
            command.Description,
            changedAtUtc);

        taskItem.ChangePriority(
            command.Priority,
            changedAtUtc);

        taskItem.ChangeDueDate(
            command.DueDateUtc,
            changedAtUtc);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return new UpdateTaskResult(
            TaskItemId: taskItem.Id,
            ProjectId: taskItem.ProjectId,
            CreatedByUserId: taskItem.CreatedByUserId,
            AssigneeId: taskItem.AssigneeId,
            Title: taskItem.Title,
            Description: taskItem.Description,
            Status: taskItem.Status,
            Priority: taskItem.Priority,
            DueDateUtc: taskItem.DueDateUtc,
            CreatedAtUtc: taskItem.CreatedAtUtc,
            UpdatedAtUtc: taskItem.UpdatedAtUtc,
            CompletedAtUtc: taskItem.CompletedAtUtc);
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