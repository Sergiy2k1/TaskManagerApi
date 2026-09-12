using TaskManager.Application.Abstractions.Authentication;
using TaskManager.Application.Abstractions.Authorization;
using TaskManager.Application.Abstractions.Messaging;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Abstractions.Time;
using TaskManager.Application.Common.Exceptions;

namespace TaskManager.Application.TaskComments.Edit;

public sealed class EditTaskCommentHandler
    : ICommandHandler<EditTaskCommentCommand, EditTaskCommentResult>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ITaskItemRepository _taskRepository;
    private readonly ITaskCommentRepository _commentRepository;
    private readonly IProjectAccessPolicy _projectAccessPolicy;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public EditTaskCommentHandler(
        IProjectRepository projectRepository,
        ITaskItemRepository taskRepository,
        ITaskCommentRepository commentRepository,
        IProjectAccessPolicy projectAccessPolicy,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IClock clock)
    {
        _projectRepository = projectRepository;
        _taskRepository = taskRepository;
        _commentRepository = commentRepository;
        _projectAccessPolicy = projectAccessPolicy;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<EditTaskCommentResult> HandleAsync(
        EditTaskCommentCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.ProjectId == Guid.Empty)
        {
            throw new ApplicationValidationException(
                "Project identifier cannot be empty.",
                nameof(command.ProjectId));
        }

        if (command.TaskItemId == Guid.Empty)
        {
            throw new ApplicationValidationException(
                "Task identifier cannot be empty.",
                nameof(command.TaskItemId));
        }

        if (command.CommentId == Guid.Empty)
        {
            throw new ApplicationValidationException(
                "Comment identifier cannot be empty.",
                nameof(command.CommentId));
        }

        var project = await _projectRepository.GetByIdAsync(
            command.ProjectId, cancellationToken);

        if (project is null)
            throw new ApplicationNotFoundException("Project was not found.");

        await _projectAccessPolicy.EnsureHasAccessAsync(
            project.OwnerId, project.Id, cancellationToken);

        if (project.IsArchived)
            throw new ApplicationConflictException(
                "Cannot edit comments in an archived project.");

        var task = await _taskRepository.GetByIdAsync(
            command.TaskItemId, cancellationToken);

        if (task is null || task.ProjectId != project.Id)
            throw new ApplicationNotFoundException("Task was not found.");

        var comment = await _commentRepository.GetByIdForUpdateAsync(
            command.CommentId, cancellationToken);

        if (comment is null ||
            comment.TaskItemId != task.Id ||
            comment.DeletedAtUtc is not null)
            throw new ApplicationNotFoundException("Comment was not found.");

        if (comment.AuthorUserId != _currentUser.UserId)
            throw new ApplicationForbiddenException(
                "Only the comment author can edit the comment.");

        comment.Edit(command.Content, _clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new EditTaskCommentResult(
            comment.Id,
            comment.TaskItemId,
            comment.AuthorUserId,
            comment.Content,
            comment.CreatedAtUtc,
            comment.UpdatedAtUtc);
    }
}
