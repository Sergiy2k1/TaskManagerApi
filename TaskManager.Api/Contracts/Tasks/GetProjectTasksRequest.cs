using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Domain.Enums;

namespace TaskManager.Api.Contracts.Tasks;

public sealed class GetProjectTasksRequest
{
    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public TaskItemStatus? Status { get; init; }

    public TaskPriority? Priority { get; init; }

    public Guid? AssigneeId { get; init; }

    public DateTimeOffset? DueFromUtc { get; init; }

    public DateTimeOffset? DueToUtc { get; init; }

    public string? Search { get; init; }

    public TaskItemSortBy SortBy { get; init; } =
        TaskItemSortBy.CreatedAt;

    public SortDirection SortDirection { get; init; } =
        SortDirection.Asc;
}
