using TaskManager.Domain.Enums;

namespace TaskManager.Application.Abstractions.Persistence;

public enum TaskItemSortBy
{
    CreatedAt = 1,
    DueDate = 2,
    Priority = 3,
    Status = 4,
    Title = 5,
    UpdatedAt = 6
}

public enum SortDirection
{
    Asc = 1,
    Desc = 2
}

public sealed record TaskItemQueryOptions(
    Guid ProjectId,
    int Page,
    int PageSize,
    TaskItemStatus? Status,
    TaskPriority? Priority,
    Guid? AssigneeId,
    DateTimeOffset? DueFromUtc,
    DateTimeOffset? DueToUtc,
    string? Search,
    TaskItemSortBy SortBy,
    SortDirection SortDirection);
