using Microsoft.EntityFrameworkCore;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Common.Pagination;
using TaskManager.Domain.Entities;

namespace TaskManager.Infrastructure.Persistence.Repositories;

public sealed class ProjectRepository : IProjectRepository
{
    private readonly AppDbContext _dbContext;

    public ProjectRepository(AppDbContext dbContext) => _dbContext = dbContext;

    public Task<Project?> GetByIdAsync(Guid projectId, CancellationToken cancellationToken = default) =>
        _dbContext.Projects.AsNoTracking().SingleOrDefaultAsync(x => x.Id == projectId, cancellationToken);

    public Task<Project?> GetByIdForUpdateAsync(Guid projectId, CancellationToken cancellationToken = default) =>
        _dbContext.Projects.SingleOrDefaultAsync(x => x.Id == projectId, cancellationToken);

    public async Task<IReadOnlyList<Project>> GetAccessibleByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await GetAccessibleQuery(userId)
            .OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc)
            .ThenBy(x => x.Name)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

    public async Task<PagedResult<Project>> GetAccessiblePageByUserIdAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = GetAccessibleQuery(userId);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc)
            .ThenBy(x => x.Name)
            .ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Project>(items, page, pageSize, totalCount);
    }

    public void Add(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);
        _dbContext.Projects.Add(project);
    }

    private IQueryable<Project> GetAccessibleQuery(Guid userId) =>
        _dbContext.Projects.AsNoTracking().Where(project =>
            project.OwnerId == userId ||
            _dbContext.ProjectMembers.Any(member =>
                member.ProjectId == project.Id &&
                member.UserId == userId &&
                member.RemovedAtUtc == null));
}
