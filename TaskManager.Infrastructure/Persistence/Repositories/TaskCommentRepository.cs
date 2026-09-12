using Microsoft.EntityFrameworkCore;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Domain.Entities;

namespace TaskManager.Infrastructure.Persistence.Repositories;

public sealed class TaskCommentRepository
    : ITaskCommentRepository
{
    private readonly AppDbContext _dbContext;

    public TaskCommentRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<TaskComment?> GetByIdForUpdateAsync(
        Guid commentId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.TaskComments
            .SingleOrDefaultAsync(
                comment => comment.Id == commentId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<TaskComment>> GetActiveByTaskAsync(
        Guid taskItemId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.TaskComments
            .AsNoTracking()
            .Where(comment =>
                comment.TaskItemId == taskItemId &&
                comment.DeletedAtUtc == null)
            .OrderBy(comment => comment.CreatedAtUtc)
            .ThenBy(comment => comment.Id)
            .ToListAsync(cancellationToken);
    }

    public void Add(TaskComment comment)
    {
        ArgumentNullException.ThrowIfNull(comment);
        _dbContext.TaskComments.Add(comment);
    }
}
