using TaskManager.Application.Abstractions.Messaging;

namespace TaskManager.Application.TaskComments.Delete;

public sealed record DeleteTaskCommentCommand(
    Guid ProjectId,
    Guid TaskItemId,
    Guid CommentId)
    : ICommand<DeleteTaskCommentResult>;
