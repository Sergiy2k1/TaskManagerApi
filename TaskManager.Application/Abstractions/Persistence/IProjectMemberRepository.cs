using TaskManager.Domain.Entities;

namespace TaskManager.Application.Abstractions.Persistence;

public interface IProjectMemberRepository
{
    Task<ProjectMember?> GetByProjectAndUserAsync(
        Guid projectId,
        Guid userId,
        CancellationToken cancellationToken = default);

    void Add(ProjectMember projectMember);
}