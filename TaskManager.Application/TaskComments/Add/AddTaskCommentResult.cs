namespace TaskManager.Application.TaskComments.Add;

public sealed record AddTaskCommentResult(
    Guid CommentId,
    Guid TaskItemId,
    Guid AuthorUserId,
    string Content,
    DateTimeOffset CreatedAtUtc);
