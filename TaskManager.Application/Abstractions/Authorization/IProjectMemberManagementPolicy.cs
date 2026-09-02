namespace TaskManager.Application.Abstractions.Authorization;

public interface IProjectMemberManagementPolicy
{
    Task EnsureCanManageMembersAsync(
        Guid projectOwnerId,
        Guid projectId,
        CancellationToken cancellationToken = default);
}