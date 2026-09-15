using TaskManager.Application.Abstractions.Messaging;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Common.Pagination;
using TaskManager.Domain.Enums;

namespace TaskManager.Application.Tasks.GetByProject;

public sealed record GetProjectTasksQuery(
    Guid ProjectId,
    int Page = 1,
    int PageSize = 20,
    TaskItemStatus? Status = null,
    TaskPriority? Priority = null,
    Guid? AssigneeId = null,
    DateTimeOffset? DueFromUtc = null,
    DateTimeOffset? DueToUtc = null,
    string? Search = null,
    TaskItemSortBy SortBy = TaskItemSortBy.CreatedAt,
    SortDirection SortDirection = SortDirection.Asc)
    : IQuery<PagedResult<GetProjectTasksResult>>;
