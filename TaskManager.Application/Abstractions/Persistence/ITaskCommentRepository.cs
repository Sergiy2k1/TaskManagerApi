using TaskManager.Domain.Entities;

namespace TaskManager.Application.Abstractions.Persistence;

public interface ITaskCommentRepository
{
    Task<TaskComment?> GetByIdForUpdateAsync(
        Guid commentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TaskComment>> GetActiveByTaskAsync(
        Guid taskItemId,
        CancellationToken cancellationToken = default);

    void Add(TaskComment comment);
}
