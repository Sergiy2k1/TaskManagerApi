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

    public void Add(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);

        _dbContext.Projects.Add(project);
    }
}