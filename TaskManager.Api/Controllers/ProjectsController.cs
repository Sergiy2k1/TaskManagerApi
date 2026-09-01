using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Api.Contracts.Projects;
using TaskManager.Application.Abstractions.Messaging;
using TaskManager.Application.Projects.AddMember;
using TaskManager.Application.Projects.Create;
using TaskManager.Application.Projects.GetById;

namespace TaskManager.Api.Controllers;

[ApiController]
[Route("api/projects")]
[Authorize]
public sealed class ProjectsController : ControllerBase
{
    private readonly ICommandHandler<
        CreateProjectCommand,
        CreateProjectResult> _createProjectHandler;

    private readonly ICommandHandler<
        AddProjectMemberCommand,
        AddProjectMemberResult> _addProjectMemberHandler;

    private readonly IQueryHandler<
        GetProjectByIdQuery,
        GetProjectByIdResult> _getProjectByIdHandler;

    public ProjectsController(
        ICommandHandler<
            CreateProjectCommand,
            CreateProjectResult> createProjectHandler,
        ICommandHandler<
            AddProjectMemberCommand,
            AddProjectMemberResult> addProjectMemberHandler,
        IQueryHandler<
            GetProjectByIdQuery,
            GetProjectByIdResult> getProjectByIdHandler)
    {
        _createProjectHandler = createProjectHandler;
        _addProjectMemberHandler = addProjectMemberHandler;
        _getProjectByIdHandler = getProjectByIdHandler;
    }

    [HttpPost]
    [ProducesResponseType(
        typeof(CreateProjectResponse),
        StatusCodes.Status201Created)]
    public async Task<ActionResult<CreateProjectResponse>> Create(
        CreateProjectRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateProjectCommand(
            Name: request.Name,
            Description: request.Description);

        var result =
            await _createProjectHandler.HandleAsync(
                command,
                cancellationToken);

        var response = new CreateProjectResponse(
            ProjectId: result.ProjectId,
            OwnerId: result.OwnerId,
            Name: result.Name,
            Description: result.Description,
            IsArchived: result.IsArchived,
            CreatedAtUtc: result.CreatedAtUtc);

        return StatusCode(
            StatusCodes.Status201Created,
            response);
    }

    [HttpPost("{projectId:guid}/members")]
    [ProducesResponseType(
        typeof(AddProjectMemberResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AddProjectMemberResponse>> AddMember(
        Guid projectId,
        AddProjectMemberRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AddProjectMemberCommand(
            ProjectId: projectId,
            Email: request.Email,
            Role: request.Role);

        var result =
            await _addProjectMemberHandler.HandleAsync(
                command,
                cancellationToken);

        var response = new AddProjectMemberResponse(
            ProjectMemberId: result.ProjectMemberId,
            ProjectId: result.ProjectId,
            UserId: result.UserId,
            Role: result.Role,
            JoinedAtUtc: result.JoinedAtUtc);

        return StatusCode(
            StatusCodes.Status201Created,
            response);
    }

    [HttpGet("{projectId:guid}")]
    [ProducesResponseType(
        typeof(GetProjectByIdResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetProjectByIdResponse>> GetById(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var query = new GetProjectByIdQuery(
            ProjectId: projectId);

        var result =
            await _getProjectByIdHandler.HandleAsync(
                query,
                cancellationToken);

        var response = new GetProjectByIdResponse(
            ProjectId: result.ProjectId,
            OwnerId: result.OwnerId,
            Name: result.Name,
            Description: result.Description,
            IsArchived: result.IsArchived,
            CreatedAtUtc: result.CreatedAtUtc,
            UpdatedAtUtc: result.UpdatedAtUtc,
            ArchivedAtUtc: result.ArchivedAtUtc);

        return Ok(response);
    }
}