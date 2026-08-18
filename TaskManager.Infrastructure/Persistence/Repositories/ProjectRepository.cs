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

    public void Add(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);

        _dbContext.Projects.Add(project);
    }
}