namespace TaskManager.Application.Abstractions.Authorization;

public interface IProjectAccessPolicy
{
    Task EnsureHasAccessAsync(
        Guid projectOwnerId,
        Guid projectId,
        CancellationToken cancellationToken = default);
}