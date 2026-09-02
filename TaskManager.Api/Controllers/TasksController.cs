using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Api.Contracts.Tasks;
using TaskManager.Application.Abstractions.Messaging;
using TaskManager.Application.Tasks.Create;
using TaskManager.Application.Tasks.GetById;
using TaskManager.Application.Tasks.GetByProject;
using TaskManager.Application.Tasks.Update;

namespace TaskManager.Api.Controllers;

[ApiController]
[Route("api/projects/{projectId:guid}/tasks")]
[Authorize]
public sealed class TasksController : ControllerBase
{
    private readonly ICommandHandler<
        CreateTaskCommand,
        CreateTaskResult> _createTaskHandler;

    private readonly IQueryHandler<
        GetTaskByIdQuery,
        GetTaskByIdResult> _getTaskByIdHandler;

    private readonly IQueryHandler<
        GetProjectTasksQuery,
        IReadOnlyList<GetProjectTasksResult>> _getProjectTasksHandler;

    private readonly ICommandHandler<
        UpdateTaskCommand,
        UpdateTaskResult> _updateTaskHandler;

    public TasksController(
        ICommandHandler<
            CreateTaskCommand,
            CreateTaskResult> createTaskHandler,
        IQueryHandler<
            GetTaskByIdQuery,
            GetTaskByIdResult> getTaskByIdHandler,
        IQueryHandler<
            GetProjectTasksQuery,
            IReadOnlyList<GetProjectTasksResult>> getProjectTasksHandler,
        ICommandHandler<
            UpdateTaskCommand,
            UpdateTaskResult> updateTaskHandler)
    {
        _createTaskHandler = createTaskHandler;
        _getTaskByIdHandler = getTaskByIdHandler;
        _getProjectTasksHandler = getProjectTasksHandler;
        _updateTaskHandler = updateTaskHandler;
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

    [HttpGet]
    [ProducesResponseType(
        typeof(IReadOnlyList<GetProjectTaskResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<
        IReadOnlyList<GetProjectTaskResponse>>> GetByProject(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var query =
            new GetProjectTasksQuery(
                ProjectId: projectId);

        var result =
            await _getProjectTasksHandler.HandleAsync(
                query,
                cancellationToken);

        var response =
            result
                .Select(
                    task =>
                        new GetProjectTaskResponse(
                            TaskItemId: task.TaskItemId,
                            ProjectId: task.ProjectId,
                            CreatedByUserId: task.CreatedByUserId,
                            AssigneeId: task.AssigneeId,
                            Title: task.Title,
                            Description: task.Description,
                            Status: task.Status,
                            Priority: task.Priority,
                            DueDateUtc: task.DueDateUtc,
                            CreatedAtUtc: task.CreatedAtUtc,
                            UpdatedAtUtc: task.UpdatedAtUtc,
                            CompletedAtUtc: task.CompletedAtUtc))
                .ToArray();

        return Ok(response);
    }

    [HttpPut("{taskItemId:guid}")]
    [ProducesResponseType(
        typeof(UpdateTaskResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UpdateTaskResponse>> Update(
        Guid projectId,
        Guid taskItemId,
        UpdateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var command =
            new UpdateTaskCommand(
                ProjectId: projectId,
                TaskItemId: taskItemId,
                Title: request.Title,
                Description: request.Description,
                Priority: request.Priority,
                DueDateUtc: request.DueDateUtc);

        var result =
            await _updateTaskHandler.HandleAsync(
                command,
                cancellationToken);

        var response =
            new UpdateTaskResponse(
                TaskItemId: result.TaskItemId,
                ProjectId: result.ProjectId,
                CreatedByUserId: result.CreatedByUserId,
                AssigneeId: result.AssigneeId,
                Title: result.Title,
                Description: result.Description,
                Status: result.Status,
                Priority: result.Priority,
                DueDateUtc: result.DueDateUtc,
                CreatedAtUtc: result.CreatedAtUtc,
                UpdatedAtUtc: result.UpdatedAtUtc,
                CompletedAtUtc: result.CompletedAtUtc);

        return Ok(response);
    }

    [HttpGet("{taskItemId:guid}")]
    [ProducesResponseType(
        typeof(GetTaskByIdResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetTaskByIdResponse>> GetById(
        Guid projectId,
        Guid taskItemId,
        CancellationToken cancellationToken)
    {
        var query =
            new GetTaskByIdQuery(
                ProjectId: projectId,
                TaskItemId: taskItemId);

        var result =
            await _getTaskByIdHandler.HandleAsync(
                query,
                cancellationToken);

        var response =
            new GetTaskByIdResponse(
                TaskItemId: result.TaskItemId,
                ProjectId: result.ProjectId,
                CreatedByUserId: result.CreatedByUserId,
                AssigneeId: result.AssigneeId,
                Title: result.Title,
                Description: result.Description,
                Status: result.Status,
                Priority: result.Priority,
                DueDateUtc: result.DueDateUtc,
                CreatedAtUtc: result.CreatedAtUtc,
                UpdatedAtUtc: result.UpdatedAtUtc,
                CompletedAtUtc: result.CompletedAtUtc);

        return Ok(response);
    }
}