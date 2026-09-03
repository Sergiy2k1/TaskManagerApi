using TaskManager.Application.Abstractions.Authorization;
using TaskManager.Application.Abstractions.Messaging;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Common.Exceptions;

namespace TaskManager.Application.Tasks.GetById;

public sealed class GetTaskByIdHandler
    : IQueryHandler<GetTaskByIdQuery, GetTaskByIdResult>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ITaskItemRepository _taskItemRepository;
    private readonly IProjectAccessPolicy _projectAccessPolicy;

    public GetTaskByIdHandler(
        IProjectRepository projectRepository,
        ITaskItemRepository taskItemRepository,
        IProjectAccessPolicy projectAccessPolicy)
    {
        _projectRepository = projectRepository;
        _taskItemRepository = taskItemRepository;
        _projectAccessPolicy = projectAccessPolicy;
    }

    public async Task<GetTaskByIdResult> HandleAsync(
        GetTaskByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.ProjectId == Guid.Empty)
        {
            throw new ApplicationValidationException(
                "Project identifier cannot be empty.",
                nameof(query.ProjectId));
        }

        if (query.TaskItemId == Guid.Empty)
        {
            throw new ApplicationValidationException(
                "Task identifier cannot be empty.",
                nameof(query.TaskItemId));
        }

        var project =
            await _projectRepository.GetByIdAsync(
                query.ProjectId,
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

        var taskItem =
            await _taskItemRepository.GetByIdAsync(
                query.TaskItemId,
                cancellationToken);

        if (taskItem is null ||
            taskItem.ProjectId != project.Id)
        {
            throw new ApplicationNotFoundException(
                "Task was not found.");
        }

        return new GetTaskByIdResult(
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
}