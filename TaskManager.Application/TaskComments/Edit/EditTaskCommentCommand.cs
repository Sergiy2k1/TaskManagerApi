using TaskManager.Application.Abstractions.Messaging;

namespace TaskManager.Application.TaskComments.Edit;

public sealed record EditTaskCommentCommand(
    Guid ProjectId,
    Guid TaskItemId,
    Guid CommentId,
    string Content)
    : ICommand<EditTaskCommentResult>;
