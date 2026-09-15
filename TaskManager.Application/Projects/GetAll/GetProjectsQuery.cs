using TaskManager.Application.Abstractions.Messaging;
using TaskManager.Application.Common.Pagination;

namespace TaskManager.Application.Projects.GetAll;

public sealed record GetProjectsQuery(int Page = 1, int PageSize = 20)
    : IQuery<PagedResult<GetProjectsResult>>;
