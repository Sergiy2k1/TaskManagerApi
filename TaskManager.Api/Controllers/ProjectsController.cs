using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Api.Contracts.Projects;
using TaskManager.Application.Projects.Create;

namespace TaskManager.Api.Controllers;

[ApiController]
[Route("api/projects")]
[Authorize]
public sealed class ProjectsController : ControllerBase
{
    private readonly CreateProjectHandler _createProjectHandler;

    public ProjectsController(
        CreateProjectHandler createProjectHandler)
    {
        _createProjectHandler = createProjectHandler;
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
}