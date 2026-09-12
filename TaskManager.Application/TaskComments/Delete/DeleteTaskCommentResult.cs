namespace TaskManager.Application.TaskComments.Delete;

public sealed record DeleteTaskCommentResult(
    Guid CommentId,
    DateTimeOffset? DeletedAtUtc);
