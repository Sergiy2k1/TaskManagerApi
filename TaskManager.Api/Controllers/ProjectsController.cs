using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Api.Contracts.Projects;
using TaskManager.Application.Projects.Create;
using TaskManager.Application.Projects.GetById;

namespace TaskManager.Api.Controllers;

[ApiController]
[Route("api/projects")]
[Authorize]
public sealed class ProjectsController : ControllerBase
{
    private readonly CreateProjectHandler _createProjectHandler;
    private readonly GetProjectByIdHandler _getProjectByIdHandler;

    public ProjectsController(
        CreateProjectHandler createProjectHandler,
        GetProjectByIdHandler getProjectByIdHandler)
    {
        _createProjectHandler = createProjectHandler;
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