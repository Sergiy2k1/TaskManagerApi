using TaskManager.Domain.Entities;

namespace TaskManager.Application.Abstractions.Persistence;

public interface ITaskItemRepository
{
    Task<TaskItem?> GetByIdAsync(
        Guid taskItemId,
        CancellationToken cancellationToken = default);

    void Add(TaskItem taskItem);
}