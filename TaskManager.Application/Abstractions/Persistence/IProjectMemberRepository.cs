using TaskManager.Application.Common.Pagination;
using TaskManager.Domain.Entities;

namespace TaskManager.Application.Abstractions.Persistence;

public interface IProjectMemberRepository
{
    Task<ProjectMember?> GetByProjectAndUserAsync(
        Guid projectId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProjectMember>> GetActiveByProjectAsync(
        Guid projectId,
        CancellationToken cancellationToken = default);

    Task<PagedResult<ProjectMember>> GetActivePageByProjectAsync(
        Guid projectId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    void Add(ProjectMember projectMember);
}
