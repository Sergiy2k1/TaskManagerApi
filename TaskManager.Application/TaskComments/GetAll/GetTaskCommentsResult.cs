namespace TaskManager.Application.TaskComments.GetAll;

public sealed record GetTaskCommentsResult(
    Guid CommentId,
    Guid TaskItemId,
    Guid AuthorUserId,
    string Content,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);
