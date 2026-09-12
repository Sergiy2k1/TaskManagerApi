using TaskManager.Application.Abstractions.Authorization;
using TaskManager.Application.Abstractions.Messaging;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Common.Exceptions;

namespace TaskManager.Application.TaskComments.GetAll;

public sealed class GetTaskCommentsHandler
    : IQueryHandler<GetTaskCommentsQuery, IReadOnlyList<GetTaskCommentsResult>>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ITaskItemRepository _taskRepository;
    private readonly ITaskCommentRepository _commentRepository;
    private readonly IProjectAccessPolicy _projectAccessPolicy;

    public GetTaskCommentsHandler(
        IProjectRepository projectRepository,
        ITaskItemRepository taskRepository,
        ITaskCommentRepository commentRepository,
        IProjectAccessPolicy projectAccessPolicy)
    {
        _projectRepository = projectRepository;
        _taskRepository = taskRepository;
        _commentRepository = commentRepository;
        _projectAccessPolicy = projectAccessPolicy;
    }

    public async Task<IReadOnlyList<GetTaskCommentsResult>> HandleAsync(
        GetTaskCommentsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.ProjectId == Guid.Empty)
            throw new ApplicationValidationException(
                "Project identifier cannot be empty.",
                nameof(query.ProjectId));

        if (query.TaskItemId == Guid.Empty)
            throw new ApplicationValidationException(
                "Task identifier cannot be empty.",
                nameof(query.TaskItemId));

        var project = await _projectRepository.GetByIdAsync(
            query.ProjectId,
            cancellationToken);

        if (project is null)
            throw new ApplicationNotFoundException("Project was not found.");

        await _projectAccessPolicy.EnsureHasAccessAsync(
            project.OwnerId,
            project.Id,
            cancellationToken);

        var task = await _taskRepository.GetByIdAsync(
            query.TaskItemId,
            cancellationToken);

        if (task is null || task.ProjectId != project.Id)
            throw new ApplicationNotFoundException("Task was not found.");

        var comments = await _commentRepository.GetActiveByTaskAsync(
            task.Id,
            cancellationToken);

        return comments.Select(comment =>
            new GetTaskCommentsResult(
                comment.Id,
                comment.TaskItemId,
                comment.AuthorUserId,
                comment.Content,
                comment.CreatedAtUtc,
                comment.UpdatedAtUtc))
            .ToList();
    }
}
