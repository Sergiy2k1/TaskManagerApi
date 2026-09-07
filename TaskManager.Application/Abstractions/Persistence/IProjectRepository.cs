using TaskManager.Domain.Entities;

namespace TaskManager.Application.Abstractions.Persistence;

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(
        Guid projectId,
        CancellationToken cancellationToken = default);

    Task<Project?> GetByIdForUpdateAsync(
        Guid projectId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Project>> GetAccessibleByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    void Add(Project project);
}
