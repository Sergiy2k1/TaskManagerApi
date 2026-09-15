using Microsoft.EntityFrameworkCore;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Common.Pagination;
using TaskManager.Domain.Entities;

namespace TaskManager.Infrastructure.Persistence.Repositories;

public sealed class TaskCommentRepository : ITaskCommentRepository
{
    private readonly AppDbContext _dbContext;

    public TaskCommentRepository(AppDbContext dbContext) => _dbContext = dbContext;

    public Task<TaskComment?> GetByIdForUpdateAsync(Guid commentId, CancellationToken cancellationToken = default) =>
        _dbContext.TaskComments.SingleOrDefaultAsync(comment => comment.Id == commentId, cancellationToken);

    public async Task<IReadOnlyList<TaskComment>> GetActiveByTaskAsync(Guid taskItemId, CancellationToken cancellationToken = default) =>
        await GetActiveQuery(taskItemId)
            .OrderBy(comment => comment.CreatedAtUtc)
            .ThenBy(comment => comment.Id)
            .ToListAsync(cancellationToken);

    public async Task<PagedResult<TaskComment>> GetActivePageByTaskAsync(Guid taskItemId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = GetActiveQuery(taskItemId);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(comment => comment.CreatedAtUtc)
            .ThenBy(comment => comment.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<TaskComment>(items, page, pageSize, totalCount);
    }

    public void Add(TaskComment comment)
    {
        ArgumentNullException.ThrowIfNull(comment);
        _dbContext.TaskComments.Add(comment);
    }

    private IQueryable<TaskComment> GetActiveQuery(Guid taskItemId) =>
        _dbContext.TaskComments
            .AsNoTracking()
            .Where(comment => comment.TaskItemId == taskItemId && comment.DeletedAtUtc == null);
}
