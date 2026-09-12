using TaskManager.Application.Abstractions.Authentication;
using TaskManager.Application.Abstractions.Authorization;
using TaskManager.Application.Abstractions.Messaging;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Abstractions.Time;
using TaskManager.Application.Common.Exceptions;

namespace TaskManager.Application.TaskComments.Delete;

public sealed class DeleteTaskCommentHandler
    : ICommandHandler<DeleteTaskCommentCommand, DeleteTaskCommentResult>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ITaskItemRepository _taskRepository;
    private readonly ITaskCommentRepository _commentRepository;
    private readonly IProjectAccessPolicy _projectAccessPolicy;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public DeleteTaskCommentHandler(
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

    public async Task<DeleteTaskCommentResult> HandleAsync(
        DeleteTaskCommentCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.ProjectId == Guid.Empty || command.TaskItemId == Guid.Empty ||
            command.CommentId == Guid.Empty)
            throw new ApplicationValidationException(
                "Identifiers cannot be empty.");

        var project = await _projectRepository.GetByIdAsync(
            command.ProjectId, cancellationToken);

        if (project is null)
            throw new ApplicationNotFoundException("Project was not found.");

        await _projectAccessPolicy.EnsureHasAccessAsync(
            project.OwnerId, project.Id, cancellationToken);

        if (project.IsArchived)
            throw new ApplicationConflictException(
                "Cannot delete comments in an archived project.");

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

        if (comment.AuthorUserId != _currentUser.UserId &&
            project.OwnerId != _currentUser.UserId)
            throw new ApplicationForbiddenException(
                "Only the comment author or project owner can delete the comment.");

        comment.Delete(_clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new DeleteTaskCommentResult(
            comment.Id,
            comment.DeletedAtUtc);
    }
}
