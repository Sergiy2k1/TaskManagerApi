using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Api.Contracts.Projects;
using TaskManager.Application.Abstractions.Messaging;
using TaskManager.Application.Projects.AddMember;
using TaskManager.Application.Projects.ChangeMemberRole;
using TaskManager.Application.Projects.Create;
using TaskManager.Application.Projects.GetById;
using TaskManager.Application.Projects.RemoveMember;

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

    private readonly ICommandHandler<
        ChangeProjectMemberRoleCommand,
        ChangeProjectMemberRoleResult> _changeProjectMemberRoleHandler;

    private readonly ICommandHandler<
        RemoveProjectMemberCommand,
        RemoveProjectMemberResult> _removeProjectMemberHandler;

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
        ICommandHandler<
            ChangeProjectMemberRoleCommand,
            ChangeProjectMemberRoleResult> changeProjectMemberRoleHandler,
        ICommandHandler<
            RemoveProjectMemberCommand,
            RemoveProjectMemberResult> removeProjectMemberHandler,
        IQueryHandler<
            GetProjectByIdQuery,
            GetProjectByIdResult> getProjectByIdHandler)
    {
        _createProjectHandler = createProjectHandler;
        _addProjectMemberHandler = addProjectMemberHandler;
        _changeProjectMemberRoleHandler = changeProjectMemberRoleHandler;
        _removeProjectMemberHandler = removeProjectMemberHandler;
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

    [HttpPatch(
        "{projectId:guid}/members/{userId:guid}/role")]
    [ProducesResponseType(
        typeof(ChangeProjectMemberRoleResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ChangeProjectMemberRoleResponse>>
        ChangeMemberRole(
            Guid projectId,
            Guid userId,
            ChangeProjectMemberRoleRequest request,
            CancellationToken cancellationToken)
    {
        var command =
            new ChangeProjectMemberRoleCommand(
                ProjectId: projectId,
                UserId: userId,
                Role: request.Role);

        var result =
            await _changeProjectMemberRoleHandler.HandleAsync(
                command,
                cancellationToken);

        var response =
            new ChangeProjectMemberRoleResponse(
                ProjectMemberId: result.ProjectMemberId,
                ProjectId: result.ProjectId,
                UserId: result.UserId,
                Role: result.Role,
                UpdatedAtUtc: result.UpdatedAtUtc);

        return Ok(response);
    }

    [HttpDelete(
        "{projectId:guid}/members/{userId:guid}")]
    [ProducesResponseType(
        StatusCodes.Status204NoContent)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RemoveMember(
        Guid projectId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var command =
            new RemoveProjectMemberCommand(
                ProjectId: projectId,
                UserId: userId);

        await _removeProjectMemberHandler.HandleAsync(
            command,
            cancellationToken);

        return NoContent();
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