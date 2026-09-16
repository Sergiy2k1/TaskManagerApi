using Microsoft.EntityFrameworkCore;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Common.Pagination;
using TaskManager.Domain.Entities;

namespace TaskManager.Infrastructure.Persistence.Repositories;

public sealed class ProjectMemberRepository : IProjectMemberRepository
{
    private readonly AppDbContext _dbContext;

    public ProjectMemberRepository(AppDbContext dbContext) => _dbContext = dbContext;

    public Task<ProjectMember?> GetByProjectAndUserAsync(Guid projectId, Guid userId, CancellationToken cancellationToken = default) =>
        _dbContext.ProjectMembers
            .AsNoTracking()
            .SingleOrDefaultAsync(
                member => member.ProjectId == projectId && member.UserId == userId,
                cancellationToken);

    public Task<ProjectMember?> GetByProjectAndUserForUpdateAsync(Guid projectId, Guid userId, CancellationToken cancellationToken = default) =>
        _dbContext.ProjectMembers.SingleOrDefaultAsync(
            member => member.ProjectId == projectId && member.UserId == userId,
            cancellationToken);

    public async Task<IReadOnlyList<ProjectMember>> GetActiveByProjectAsync(Guid projectId, CancellationToken cancellationToken = default) =>
        await GetActiveQuery(projectId)
            .OrderBy(member => member.JoinedAtUtc)
            .ThenBy(member => member.UserId)
            .ToListAsync(cancellationToken);

    public async Task<PagedResult<ProjectMember>> GetActivePageByProjectAsync(Guid projectId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = GetActiveQuery(projectId);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(member => member.JoinedAtUtc)
            .ThenBy(member => member.UserId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ProjectMember>(items, page, pageSize, totalCount);
    }

    public void Add(ProjectMember projectMember)
    {
        ArgumentNullException.ThrowIfNull(projectMember);
        _dbContext.ProjectMembers.Add(projectMember);
    }

    private IQueryable<ProjectMember> GetActiveQuery(Guid projectId) =>
        _dbContext.ProjectMembers
            .AsNoTracking()
            .Where(member => member.ProjectId == projectId && member.RemovedAtUtc == null);
}
