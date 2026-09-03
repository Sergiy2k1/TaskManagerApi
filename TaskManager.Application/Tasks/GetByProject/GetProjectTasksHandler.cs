using TaskManager.Application.Abstractions.Authorization;
using TaskManager.Application.Abstractions.Messaging;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Common.Exceptions;

namespace TaskManager.Application.Tasks.GetByProject;

public sealed class GetProjectTasksHandler
    : IQueryHandler<
        GetProjectTasksQuery,
        IReadOnlyList<GetProjectTasksResult>>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ITaskItemRepository _taskItemRepository;
    private readonly IProjectAccessPolicy _projectAccessPolicy;

    public GetProjectTasksHandler(
        IProjectRepository projectRepository,
        ITaskItemRepository taskItemRepository,
        IProjectAccessPolicy projectAccessPolicy)
    {
        _projectRepository = projectRepository;
        _taskItemRepository = taskItemRepository;
        _projectAccessPolicy = projectAccessPolicy;
    }

    public async Task<IReadOnlyList<GetProjectTasksResult>> HandleAsync(
        GetProjectTasksQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.ProjectId == Guid.Empty)
        {
            throw new ApplicationValidationException(
                "Project identifier cannot be empty.",
                nameof(query.ProjectId));
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

        var taskItems =
            await _taskItemRepository.GetByProjectAsync(
                project.Id,
                cancellationToken);

        return taskItems
            .Select(
                taskItem =>
                    new GetProjectTasksResult(
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
                        CompletedAtUtc: taskItem.CompletedAtUtc))
            .ToArray();
    }
}