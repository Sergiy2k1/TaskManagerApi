using TaskManager.Application.Abstractions.Authentication;
using TaskManager.Application.Abstractions.Authorization;
using TaskManager.Application.Abstractions.Messaging;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Abstractions.Time;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Domain.Entities;

namespace TaskManager.Application.TaskComments.Add;

public sealed class AddTaskCommentHandler
    : ICommandHandler<AddTaskCommentCommand, AddTaskCommentResult>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ITaskItemRepository _taskRepository;
    private readonly ITaskCommentRepository _commentRepository;
    private readonly IProjectAccessPolicy _projectAccessPolicy;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public AddTaskCommentHandler(
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

    public async Task<AddTaskCommentResult> HandleAsync(
        AddTaskCommentCommand command,
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

        var project = await GetAccessibleProjectAsync(
            command.ProjectId,
            cancellationToken);

        if (project.IsArchived)
        {
            throw new ApplicationConflictException(
                "Cannot add comments to an archived project.");
        }

        var task = await GetTaskAsync(
            command.TaskItemId,
            project.Id,
            cancellationToken);

        var comment = TaskComment.Create(
            task.Id,
            _currentUser.UserId,
            command.Content,
            _clock.UtcNow);

        _commentRepository.Add(comment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AddTaskCommentResult(
            comment.Id,
            comment.TaskItemId,
            comment.AuthorUserId,
            comment.Content,
            comment.CreatedAtUtc);
    }

    private async Task<Project> GetAccessibleProjectAsync(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(
            projectId,
            cancellationToken);

        if (project is null)
            throw new ApplicationNotFoundException("Project was not found.");

        await _projectAccessPolicy.EnsureHasAccessAsync(
            project.OwnerId,
            project.Id,
            cancellationToken);

        return project;
    }

    private async Task<TaskItem> GetTaskAsync(
        Guid taskItemId,
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var task = await _taskRepository.GetByIdAsync(
            taskItemId,
            cancellationToken);

        if (task is null || task.ProjectId != projectId)
            throw new ApplicationNotFoundException("Task was not found.");

        return task;
    }
}
