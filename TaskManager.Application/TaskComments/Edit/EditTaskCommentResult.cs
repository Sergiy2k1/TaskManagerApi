namespace TaskManager.Application.TaskComments.Edit;

public sealed record EditTaskCommentResult(
    Guid CommentId,
    Guid TaskItemId,
    Guid AuthorUserId,
    string Content,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);
