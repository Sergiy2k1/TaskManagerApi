using TaskManager.Application.Abstractions.Authentication;
using TaskManager.Application.Abstractions.Messaging;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Common.Exceptions;

namespace TaskManager.Application.Tasks.GetById;

public sealed class GetTaskByIdHandler
    : IQueryHandler<GetTaskByIdQuery, GetTaskByIdResult>
{
    private readonly IProjectRepository _projectRepository;
    private readonly IProjectMemberRepository _projectMemberRepository;
    private readonly ITaskItemRepository _taskItemRepository;
    private readonly ICurrentUser _currentUser;

    public GetTaskByIdHandler(
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