using Microsoft.EntityFrameworkCore;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Domain.Entities;

namespace TaskManager.Infrastructure.Persistence.Repositories;

public sealed class ProjectMemberRepository
    : IProjectMemberRepository
{
    private readonly AppDbContext _dbContext;

    public ProjectMemberRepository(
        AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<ProjectMember?> GetByProjectAndUserAsync(
        Guid projectId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ProjectMembers
            .SingleOrDefaultAsync(
                member =>
                    member.ProjectId == projectId &&
                    member.UserId == userId,
                cancellationToken);
    }

    public void Add(
        ProjectMember projectMember)
    {
        ArgumentNullException.ThrowIfNull(
            projectMember);

        _dbContext.ProjectMembers.Add(
            projectMember);
    }
}