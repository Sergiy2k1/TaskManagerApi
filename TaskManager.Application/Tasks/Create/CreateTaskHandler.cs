using TaskManager.Application.Abstractions.Authentication;
using TaskManager.Application.Abstractions.Authorization;
using TaskManager.Application.Abstractions.Messaging;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Abstractions.Time;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Domain.Entities;

namespace TaskManager.Application.Tasks.Create;

public sealed class CreateTaskHandler
    : ICommandHandler<CreateTaskCommand, CreateTaskResult>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ITaskItemRepository _taskItemRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IProjectAccessPolicy _projectAccessPolicy;
    private readonly IClock _clock;

    public CreateTaskHandler(
        IProjectRepository projectRepository,
        ITaskItemRepository taskItemRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IProjectAccessPolicy projectAccessPolicy,
        IClock clock)
    {
        _projectRepository = projectRepository;
        _taskItemRepository = taskItemRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _projectAccessPolicy = projectAccessPolicy;
        _clock = clock;
    }

    public async Task<CreateTaskResult> HandleAsync(
        CreateTaskCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.ProjectId == Guid.Empty)
        {
            throw new ApplicationValidationException(
                "Project identifier cannot be empty.",
                nameof(command.ProjectId));
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

        await _projectAccessPolicy.EnsureHasAccessAsync(
            project.OwnerId,
            project.Id,
            cancellationToken);

        if (project.IsArchived)
        {
            throw new ApplicationConflictException(
                "Cannot create tasks in an archived project.");
        }

        var createdAtUtc =
            _clock.UtcNow;

        var taskItem =
            TaskItem.Create(
                projectId: project.Id,
                createdByUserId: _currentUser.UserId,
                title: command.Title,
                description: command.Description,
                priority: command.Priority,
                dueDateUtc: command.DueDateUtc,
                createdAtUtc: createdAtUtc);

        _taskItemRepository.Add(
            taskItem);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return new CreateTaskResult(
            TaskItemId: taskItem.Id,
            ProjectId: taskItem.ProjectId,
            CreatedByUserId: taskItem.CreatedByUserId,
            AssigneeId: taskItem.AssigneeId,
            Title: taskItem.Title,
            Description: taskItem.Description,
            Status: taskItem.Status,
            Priority: taskItem.Priority,
            DueDateUtc: taskItem.DueDateUtc,
            CreatedAtUtc: taskItem.CreatedAtUtc);
    }
}