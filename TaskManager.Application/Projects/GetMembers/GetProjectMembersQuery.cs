using TaskManager.Application.Abstractions.Messaging;
using TaskManager.Application.Common.Pagination;

namespace TaskManager.Application.Projects.GetMembers;

public sealed record GetProjectMembersQuery(Guid ProjectId, int Page = 1, int PageSize = 20)
    : IQuery<PagedResult<GetProjectMembersResult>>;
