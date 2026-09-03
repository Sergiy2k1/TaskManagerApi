using TaskManager.Application.Abstractions.Authorization;
using TaskManager.Application.Abstractions.Messaging;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Abstractions.Time;
using TaskManager.Application.Common.Exceptions;

namespace TaskManager.Application.Tasks.Unassign;

public sealed class UnassignTaskHandler
    : ICommandHandler<UnassignTaskCommand, UnassignTaskResult>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ITaskItemRepository _taskItemRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProjectAccessPolicy _projectAccessPolicy;
    private readonly IClock _clock;

    public UnassignTaskHandler(
        IProjectRepository projectRepository,
        ITaskItemRepository taskItemRepository,
        IUnitOfWork unitOfWork,
        IProjectAccessPolicy projectAccessPolicy,
        IClock clock)
    {
        _projectRepository = projectRepository;
        _taskItemRepository = taskItemRepository;
        _unitOfWork = unitOfWork;
        _projectAccessPolicy = projectAccessPolicy;
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

        await _projectAccessPolicy.EnsureHasAccessAsync(
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
}