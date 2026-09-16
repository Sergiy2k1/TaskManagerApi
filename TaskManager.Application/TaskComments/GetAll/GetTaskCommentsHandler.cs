using TaskManager.Application.Abstractions.Authorization;
using TaskManager.Application.Abstractions.Messaging;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Common.Pagination;

namespace TaskManager.Application.TaskComments.GetAll;

public sealed class GetTaskCommentsHandler : IQueryHandler<GetTaskCommentsQuery, PagedResult<GetTaskCommentsResult>>
{
    private const int MaxPageSize = 100;
    private readonly IProjectRepository _projectRepository;
    private readonly ITaskItemRepository _taskRepository;
    private readonly ITaskCommentRepository _commentRepository;
    private readonly IProjectAccessPolicy _projectAccessPolicy;

    public GetTaskCommentsHandler(IProjectRepository projectRepository, ITaskItemRepository taskRepository, ITaskCommentRepository commentRepository, IProjectAccessPolicy projectAccessPolicy)
    {
        _projectRepository = projectRepository;
        _taskRepository = taskRepository;
        _commentRepository = commentRepository;
        _projectAccessPolicy = projectAccessPolicy;
    }

    public async Task<PagedResult<GetTaskCommentsResult>> HandleAsync(GetTaskCommentsQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.ProjectId == Guid.Empty) throw new ApplicationValidationException("Project identifier cannot be empty.", nameof(query.ProjectId));
        if (query.TaskItemId == Guid.Empty) throw new ApplicationValidationException("Task identifier cannot be empty.", nameof(query.TaskItemId));
        Validate(query.Page, query.PageSize);

        var project = await _projectRepository.GetByIdAsync(query.ProjectId, cancellationToken);
        if (project is null) throw new ApplicationNotFoundException("Project was not found.");

        await _projectAccessPolicy.EnsureHasAccessAsync(project.OwnerId, project.Id, cancellationToken);

        var task = await _taskRepository.GetByIdReadOnlyAsync(query.TaskItemId, cancellationToken);
        if (task is null || task.ProjectId != project.Id) throw new ApplicationNotFoundException("Task was not found.");

        var page = await _commentRepository.GetActivePageByTaskAsync(task.Id, query.Page, query.PageSize, cancellationToken);
        var items = page.Items.Select(comment => new GetTaskCommentsResult(
            comment.Id,
            comment.TaskItemId,
            comment.AuthorUserId,
            comment.Content,
            comment.CreatedAtUtc,
            comment.UpdatedAtUtc)).ToArray();

        return new PagedResult<GetTaskCommentsResult>(items, page.Page, page.PageSize, page.TotalCount);
    }

    private static void Validate(int page, int pageSize)
    {
        if (page < 1) throw new ApplicationValidationException("Page must be greater than or equal to 1.", nameof(page));
        if (pageSize is < 1 or > MaxPageSize) throw new ApplicationValidationException($"Page size must be between 1 and {MaxPageSize}.", nameof(pageSize));
        if ((long)(page - 1) * pageSize > int.MaxValue) throw new ApplicationValidationException("Requested page is too large.", nameof(page));
    }
}
