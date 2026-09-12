using TaskManager.Application.Abstractions.Messaging;

namespace TaskManager.Application.TaskComments.Add;

public sealed record AddTaskCommentCommand(
    Guid ProjectId,
    Guid TaskItemId,
    string Content)
    : ICommand<AddTaskCommentResult>;
