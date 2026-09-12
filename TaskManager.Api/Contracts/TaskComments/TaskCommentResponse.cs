namespace TaskManager.Api.Contracts.TaskComments;

public sealed record TaskCommentResponse(
    Guid CommentId,
    Guid TaskItemId,
    Guid AuthorUserId,
    string Content,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);
