using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Api.Contracts.TaskComments;
using TaskManager.Application.Abstractions.Messaging;
using TaskManager.Application.TaskComments.Add;
using TaskManager.Application.TaskComments.Delete;
using TaskManager.Application.TaskComments.Edit;
using TaskManager.Application.TaskComments.GetAll;

namespace TaskManager.Api.Controllers;

[ApiController]
[Route("api/projects/{projectId:guid}/tasks/{taskItemId:guid}/comments")]
[Authorize]
public sealed class TaskCommentsController : ControllerBase
{
    private readonly ICommandHandler<AddTaskCommentCommand, AddTaskCommentResult> _add;
    private readonly IQueryHandler<GetTaskCommentsQuery, IReadOnlyList<GetTaskCommentsResult>> _getAll;
    private readonly ICommandHandler<EditTaskCommentCommand, EditTaskCommentResult> _edit;
    private readonly ICommandHandler<DeleteTaskCommentCommand, DeleteTaskCommentResult> _delete;

    public TaskCommentsController(
        ICommandHandler<AddTaskCommentCommand, AddTaskCommentResult> add,
        IQueryHandler<GetTaskCommentsQuery, IReadOnlyList<GetTaskCommentsResult>> getAll,
        ICommandHandler<EditTaskCommentCommand, EditTaskCommentResult> edit,
        ICommandHandler<DeleteTaskCommentCommand, DeleteTaskCommentResult> delete)
    {
        _add = add;
        _getAll = getAll;
        _edit = edit;
        _delete = delete;
    }

    [HttpPost]
    public async Task<ActionResult<TaskCommentResponse>> Add(
        Guid projectId,
        Guid taskItemId,
        AddTaskCommentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _add.HandleAsync(
            new AddTaskCommentCommand(projectId, taskItemId, request.Content),
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            new TaskCommentResponse(
                result.CommentId,
                result.TaskItemId,
                result.AuthorUserId,
                result.Content,
                result.CreatedAtUtc,
                null));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TaskCommentResponse>>> GetAll(
        Guid projectId,
        Guid taskItemId,
        CancellationToken cancellationToken)
    {
        var result = await _getAll.HandleAsync(
            new GetTaskCommentsQuery(projectId, taskItemId),
            cancellationToken);

        return Ok(result.Select(comment =>
            new TaskCommentResponse(
                comment.CommentId,
                comment.TaskItemId,
                comment.AuthorUserId,
                comment.Content,
                comment.CreatedAtUtc,
                comment.UpdatedAtUtc))
            .ToList());
    }

    [HttpPatch("{commentId:guid}")]
    public async Task<ActionResult<TaskCommentResponse>> Edit(
        Guid projectId,
        Guid taskItemId,
        Guid commentId,
        EditTaskCommentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _edit.HandleAsync(
            new EditTaskCommentCommand(
                projectId,
                taskItemId,
                commentId,
                request.Content),
            cancellationToken);

        return Ok(new TaskCommentResponse(
            result.CommentId,
            result.TaskItemId,
            result.AuthorUserId,
            result.Content,
            result.CreatedAtUtc,
            result.UpdatedAtUtc));
    }

    [HttpDelete("{commentId:guid}")]
    public async Task<IActionResult> Delete(
        Guid projectId,
        Guid taskItemId,
        Guid commentId,
        CancellationToken cancellationToken)
    {
        await _delete.HandleAsync(
            new DeleteTaskCommentCommand(
                projectId,
                taskItemId,
                commentId),
            cancellationToken);

        return NoContent();
    }
}
