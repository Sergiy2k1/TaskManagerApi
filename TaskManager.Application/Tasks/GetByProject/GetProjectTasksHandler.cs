using TaskManager.Application.Abstractions.Authorization;
using TaskManager.Application.Abstractions.Messaging;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Common.Pagination;

namespace TaskManager.Application.Tasks.GetByProject;

public sealed class GetProjectTasksHandler
    : IQueryHandler<
        GetProjectTasksQuery,
        PagedResult<GetProjectTasksResult>>
{
    private const int MaxPageSize = 100;
    private const int MaxSearchLength = 200;

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

    public async Task<PagedResult<GetProjectTasksResult>> HandleAsync(
        GetProjectTasksQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        Validate(query);

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

        var search =
            string.IsNullOrWhiteSpace(query.Search)
                ? null
                : query.Search.Trim();

        var options =
            new TaskItemQueryOptions(
                ProjectId: project.Id,
                Page: query.Page,
                PageSize: query.PageSize,
                Status: query.Status,
                Priority: query.Priority,
                AssigneeId: query.AssigneeId,
                DueFromUtc: query.DueFromUtc?.ToUniversalTime(),
                DueToUtc: query.DueToUtc?.ToUniversalTime(),
                Search: search,
                SortBy: query.SortBy,
                SortDirection: query.SortDirection);

        var taskPage =
            await _taskItemRepository.GetPageByProjectAsync(
                options,
                cancellationToken);

        var items =
            taskPage.Items
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

        return new PagedResult<GetProjectTasksResult>(
            items,
            taskPage.Page,
            taskPage.PageSize,
            taskPage.TotalCount);
    }

    private static void Validate(
        GetProjectTasksQuery query)
    {
        if (query.ProjectId == Guid.Empty)
        {
            throw new ApplicationValidationException(
                "Project identifier cannot be empty.",
                nameof(query.ProjectId));
        }

        if (query.Page < 1)
        {
            throw new ApplicationValidationException(
                "Page must be greater than or equal to 1.",
                nameof(query.Page));
        }

        if (query.PageSize is < 1 or > MaxPageSize)
        {
            throw new ApplicationValidationException(
                $"Page size must be between 1 and {MaxPageSize}.",
                nameof(query.PageSize));
        }

        if (!Enum.IsDefined(query.SortBy))
        {
            throw new ApplicationValidationException(
                $"Unsupported task sort field: {query.SortBy}.",
                nameof(query.SortBy));
        }

        if (!Enum.IsDefined(query.SortDirection))
        {
            throw new ApplicationValidationException(
                $"Unsupported sort direction: {query.SortDirection}.",
                nameof(query.SortDirection));
        }

        if (query.Status.HasValue &&
            !Enum.IsDefined(query.Status.Value))
        {
            throw new ApplicationValidationException(
                $"Unsupported task status: {query.Status}.",
                nameof(query.Status));
        }

        if (query.Priority.HasValue &&
            !Enum.IsDefined(query.Priority.Value))
        {
            throw new ApplicationValidationException(
                $"Unsupported task priority: {query.Priority}.",
                nameof(query.Priority));
        }

        if (query.AssigneeId == Guid.Empty)
        {
            throw new ApplicationValidationException(
                "Assignee identifier cannot be empty.",
                nameof(query.AssigneeId));
        }

        if (query.DueFromUtc.HasValue &&
            query.DueToUtc.HasValue &&
            query.DueFromUtc.Value >
            query.DueToUtc.Value)
        {
            throw new ApplicationValidationException(
                "Due date range is invalid.",
                nameof(query.DueFromUtc));
        }

        if (!string.IsNullOrWhiteSpace(query.Search) &&
            query.Search.Trim().Length > MaxSearchLength)
        {
            throw new ApplicationValidationException(
                $"Search text cannot exceed {MaxSearchLength} characters.",
                nameof(query.Search));
        }

        var skip =
            (long)(query.Page - 1) *
            query.PageSize;

        if (skip > int.MaxValue)
        {
            throw new ApplicationValidationException(
                "Requested page is too large.",
                nameof(query.Page));
        }
    }
}
