using TaskManager.Application.Abstractions.Messaging;
using TaskManager.Application.Common.Pagination;

namespace TaskManager.Application.TaskComments.GetAll;

public sealed record GetTaskCommentsQuery(Guid ProjectId, Guid TaskItemId, int Page = 1, int PageSize = 20)
    : IQuery<PagedResult<GetTaskCommentsResult>>;
