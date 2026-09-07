using Microsoft.EntityFrameworkCore;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Domain.Entities;

namespace TaskManager.Infrastructure.Persistence.Repositories;

public sealed class ProjectRepository
    : IProjectRepository
{
    private readonly AppDbContext _dbContext;

    public ProjectRepository(
        AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Project?> GetByIdAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Projects
            .AsNoTracking()
            .SingleOrDefaultAsync(
                project => project.Id == projectId,
                cancellationToken);
    }

    public Task<Project?> GetByIdForUpdateAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Projects
            .SingleOrDefaultAsync(
                project => project.Id == projectId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Project>> GetAccessibleByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Projects
            .AsNoTracking()
            .Where(
                project =>
                    project.OwnerId == userId ||
                    _dbContext.ProjectMembers.Any(
                        member =>
                            member.ProjectId == project.Id &&
                            member.UserId == userId &&
                            member.IsActive))
            .OrderByDescending(project => project.UpdatedAtUtc ?? project.CreatedAtUtc)
            .ThenBy(project => project.Name)
            .ToListAsync(cancellationToken);
    }

    public void Add(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);

        _dbContext.Projects.Add(project);
    }
}
