namespace TaskManager.Api.Contracts.Tasks;

public sealed record GetProjectTasksPageResponse(
    IReadOnlyList<GetProjectTaskResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
