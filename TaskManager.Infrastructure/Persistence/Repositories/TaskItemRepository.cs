using Microsoft.EntityFrameworkCore;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Domain.Entities;

namespace TaskManager.Infrastructure.Persistence.Repositories;

public sealed class TaskItemRepository
    : ITaskItemRepository
{
    private readonly AppDbContext _dbContext;

    public TaskItemRepository(
        AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<TaskItem?> GetByIdAsync(
        Guid taskItemId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.TaskItems
            .SingleOrDefaultAsync(
                taskItem => taskItem.Id == taskItemId,
                cancellationToken);
    }

    public void Add(
        TaskItem taskItem)
    {
        ArgumentNullException.ThrowIfNull(
            taskItem);

        _dbContext.TaskItems.Add(
            taskItem);
    }
}