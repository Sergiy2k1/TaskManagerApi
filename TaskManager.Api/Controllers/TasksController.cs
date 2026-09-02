using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Api.Contracts.Tasks;
using TaskManager.Application.Abstractions.Messaging;
using TaskManager.Application.Tasks.Create;

namespace TaskManager.Api.Controllers;

[ApiController]
[Route("api/projects/{projectId:guid}/tasks")]
[Authorize]
public sealed class TasksController : ControllerBase
{
    private readonly ICommandHandler<
        CreateTaskCommand,
        CreateTaskResult> _createTaskHandler;

    public TasksController(
        ICommandHandler<
            CreateTaskCommand,
            CreateTaskResult> createTaskHandler)
    {
        _createTaskHandler = createTaskHandler;
    }

    [HttpPost]
    [ProducesResponseType(
        typeof(CreateTaskResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreateTaskResponse>> Create(
        Guid projectId,
        CreateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var command =
            new CreateTaskCommand(
                ProjectId: projectId,
                Title: request.Title,
                Description: request.Description,
                Priority: request.Priority,
                DueDateUtc: request.DueDateUtc);

        var result =
            await _createTaskHandler.HandleAsync(
                command,
                cancellationToken);

        var response =
            new CreateTaskResponse(
                TaskItemId: result.TaskItemId,
                ProjectId: result.ProjectId,
                CreatedByUserId: result.CreatedByUserId,
                AssigneeId: result.AssigneeId,
                Title: result.Title,
                Description: result.Description,
                Status: result.Status,
                Priority: result.Priority,
                DueDateUtc: result.DueDateUtc,
                CreatedAtUtc: result.CreatedAtUtc);

        return StatusCode(
            StatusCodes.Status201Created,
            response);
    }
}