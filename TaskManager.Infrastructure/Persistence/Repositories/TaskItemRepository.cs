using Microsoft.EntityFrameworkCore;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Common.Pagination;
using TaskManager.Domain.Entities;

namespace TaskManager.Infrastructure.Persistence.Repositories;

public sealed class TaskItemRepository
    : ITaskItemRepository
{
    private readonly AppDbContext _dbContext;

    public TaskItemRepository(
        AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public TaskItemRepository()
    {
        throw new NotSupportedException();
    }

    public Task<TaskItem?> GetByIdAsync(
        Guid taskItemId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.TaskItems
            .SingleOrDefaultAsync(
                taskItem => taskItem.Id == taskItemId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<TaskItem>> GetByProjectAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.TaskItems
            .AsNoTracking()
            .Where(
                taskItem => taskItem.ProjectId == projectId)
            .OrderBy(
                taskItem => taskItem.CreatedAtUtc)
            .ThenBy(
                taskItem => taskItem.Id)
            .ToListAsync(
                cancellationToken);
    }

    public async Task<PagedResult<TaskItem>> GetPageByProjectAsync(
        TaskItemQueryOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        var query =
            _dbContext.TaskItems
                .AsNoTracking()
                .Where(
                    taskItem =>
                        taskItem.ProjectId ==
                        options.ProjectId);

        if (options.Status.HasValue)
        {
            query =
                query.Where(
                    taskItem =>
                        taskItem.Status ==
                        options.Status.Value);
        }

        if (options.Priority.HasValue)
        {
            query =
                query.Where(
                    taskItem =>
                        taskItem.Priority ==
                        options.Priority.Value);
        }

        if (options.AssigneeId.HasValue)
        {
            query =
                query.Where(
                    taskItem =>
                        taskItem.AssigneeId ==
                        options.AssigneeId.Value);
        }

        if (options.DueFromUtc.HasValue)
        {
            query =
                query.Where(
                    taskItem =>
                        taskItem.DueDateUtc.HasValue &&
                        taskItem.DueDateUtc.Value >=
                        options.DueFromUtc.Value);
        }

        if (options.DueToUtc.HasValue)
        {
            query =
                query.Where(
                    taskItem =>
                        taskItem.DueDateUtc.HasValue &&
                        taskItem.DueDateUtc.Value <=
                        options.DueToUtc.Value);
        }

        if (!string.IsNullOrWhiteSpace(
                options.Search))
        {
            var pattern =
                $"%{options.Search.Trim()}%";

            query =
                query.Where(
                    taskItem =>
                        EF.Functions.ILike(
                            taskItem.Title,
                            pattern) ||
                        (taskItem.Description != null &&
                         EF.Functions.ILike(
                             taskItem.Description,
                             pattern)));
        }

        var totalCount =
            await query.CountAsync(
                cancellationToken);

        var orderedQuery =
            ApplyOrdering(
                query,
                options.SortBy,
                options.SortDirection);

        var skip =
            (options.Page - 1) *
            options.PageSize;

        var items =
            await orderedQuery
                .Skip(skip)
                .Take(options.PageSize)
                .ToListAsync(cancellationToken);

        return new PagedResult<TaskItem>(
            items,
            options.Page,
            options.PageSize,
            totalCount);
    }

    public void Add(
        TaskItem taskItem)
    {
        ArgumentNullException.ThrowIfNull(
            taskItem);

        _dbContext.TaskItems.Add(
            taskItem);
    }

    private static IOrderedQueryable<TaskItem>
        ApplyOrdering(
            IQueryable<TaskItem> query,
            TaskItemSortBy sortBy,
            SortDirection sortDirection)
    {
        return (sortBy, sortDirection) switch
        {
            (TaskItemSortBy.CreatedAt, SortDirection.Asc) =>
                query
                    .OrderBy(taskItem => taskItem.CreatedAtUtc)
                    .ThenBy(taskItem => taskItem.Id),

            (TaskItemSortBy.CreatedAt, SortDirection.Desc) =>
                query
                    .OrderByDescending(taskItem => taskItem.CreatedAtUtc)
                    .ThenBy(taskItem => taskItem.Id),

            (TaskItemSortBy.DueDate, SortDirection.Asc) =>
                query
                    .OrderBy(taskItem => taskItem.DueDateUtc == null)
                    .ThenBy(taskItem => taskItem.DueDateUtc)
                    .ThenBy(taskItem => taskItem.Id),

            (TaskItemSortBy.DueDate, SortDirection.Desc) =>
                query
                    .OrderBy(taskItem => taskItem.DueDateUtc == null)
                    .ThenByDescending(taskItem => taskItem.DueDateUtc)
                    .ThenBy(taskItem => taskItem.Id),

            (TaskItemSortBy.Priority, SortDirection.Asc) =>
                query
                    .OrderBy(taskItem => taskItem.Priority)
                    .ThenBy(taskItem => taskItem.Id),

            (TaskItemSortBy.Priority, SortDirection.Desc) =>
                query
                    .OrderByDescending(taskItem => taskItem.Priority)
                    .ThenBy(taskItem => taskItem.Id),

            (TaskItemSortBy.Status, SortDirection.Asc) =>
                query
                    .OrderBy(taskItem => taskItem.Status)
                    .ThenBy(taskItem => taskItem.Id),

            (TaskItemSortBy.Status, SortDirection.Desc) =>
                query
                    .OrderByDescending(taskItem => taskItem.Status)
                    .ThenBy(taskItem => taskItem.Id),

            (TaskItemSortBy.Title, SortDirection.Asc) =>
                query
                    .OrderBy(taskItem => taskItem.Title)
                    .ThenBy(taskItem => taskItem.Id),

            (TaskItemSortBy.Title, SortDirection.Desc) =>
                query
                    .OrderByDescending(taskItem => taskItem.Title)
                    .ThenBy(taskItem => taskItem.Id),

            (TaskItemSortBy.UpdatedAt, SortDirection.Asc) =>
                query
                    .OrderBy(
                        taskItem =>
                            taskItem.UpdatedAtUtc ??
                            taskItem.CreatedAtUtc)
                    .ThenBy(taskItem => taskItem.Id),

            (TaskItemSortBy.UpdatedAt, SortDirection.Desc) =>
                query
                    .OrderByDescending(
                        taskItem =>
                            taskItem.UpdatedAtUtc ??
                            taskItem.CreatedAtUtc)
                    .ThenBy(taskItem => taskItem.Id),

            _ =>
                query
                    .OrderBy(taskItem => taskItem.CreatedAtUtc)
                    .ThenBy(taskItem => taskItem.Id)
        };
    }
}
