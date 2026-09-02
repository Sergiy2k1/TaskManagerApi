using TaskManager.Application.Abstractions.Authentication;
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
    private readonly IProjectMemberRepository _projectMemberRepository;
    private readonly ITaskItemRepository _taskItemRepository;
    private readonly ICurrentUser _currentUser;

    public GetProjectTasksHandler(
        IProjectRepository projectRepository,
        IProjectMemberRepository projectMemberRepository,
        ITaskItemRepository taskItemRepository,
        ICurrentUser currentUser)
    {
        _projectRepository = projectRepository;
        _projectMemberRepository = projectMemberRepository;
        _taskItemRepository = taskItemRepository;
        _currentUser = currentUser;
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

        if (project.OwnerId != _currentUser.UserId)
        {
            var currentMember =
                await _projectMemberRepository
                    .GetByProjectAndUserAsync(
                        project.Id,
                        _currentUser.UserId,
                        cancellationToken);

            if (currentMember is null ||
                !currentMember.IsActive)
            {
                throw new ApplicationNotFoundException(
                    "Project was not found.");
            }
        }

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