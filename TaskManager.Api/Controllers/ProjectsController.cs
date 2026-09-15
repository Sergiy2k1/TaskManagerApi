using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Api.Contracts.Common;
using TaskManager.Api.Contracts.Projects;
using TaskManager.Application.Abstractions.Messaging;
using TaskManager.Application.Common.Pagination;
using TaskManager.Application.Projects.AddMember;
using TaskManager.Application.Projects.Archive;
using TaskManager.Application.Projects.ChangeMemberRole;
using TaskManager.Application.Projects.Create;
using TaskManager.Application.Projects.GetAll;
using TaskManager.Application.Projects.GetById;
using TaskManager.Application.Projects.GetMembers;
using TaskManager.Application.Projects.RemoveMember;
using TaskManager.Application.Projects.Restore;
using TaskManager.Application.Projects.Update;

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
        UpdateProjectCommand,
        UpdateProjectResult> _updateProjectHandler;

    private readonly ICommandHandler<
        ArchiveProjectCommand,
        ArchiveProjectResult> _archiveProjectHandler;

    private readonly ICommandHandler<
        RestoreProjectCommand,
        RestoreProjectResult> _restoreProjectHandler;

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

    private readonly IQueryHandler<
        GetProjectsQuery,
        PagedResult<GetProjectsResult>> _getProjectsHandler;

    private readonly IQueryHandler<
        GetProjectMembersQuery,
        PagedResult<GetProjectMembersResult>> _getProjectMembersHandler;

    public ProjectsController(
        ICommandHandler<
            CreateProjectCommand,
            CreateProjectResult> createProjectHandler,
        ICommandHandler<
            UpdateProjectCommand,
            UpdateProjectResult> updateProjectHandler,
        ICommandHandler<
            ArchiveProjectCommand,
            ArchiveProjectResult> archiveProjectHandler,
        ICommandHandler<
            RestoreProjectCommand,
            RestoreProjectResult> restoreProjectHandler,
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
            GetProjectByIdResult> getProjectByIdHandler,
        IQueryHandler<
            GetProjectsQuery,
            PagedResult<GetProjectsResult>> getProjectsHandler,
        IQueryHandler<
            GetProjectMembersQuery,
            PagedResult<GetProjectMembersResult>> getProjectMembersHandler)
    {
        _createProjectHandler = createProjectHandler;
        _updateProjectHandler = updateProjectHandler;
        _archiveProjectHandler = archiveProjectHandler;
        _restoreProjectHandler = restoreProjectHandler;
        _addProjectMemberHandler = addProjectMemberHandler;
        _changeProjectMemberRoleHandler = changeProjectMemberRoleHandler;
        _removeProjectMemberHandler = removeProjectMemberHandler;
        _getProjectByIdHandler = getProjectByIdHandler;
        _getProjectsHandler = getProjectsHandler;
        _getProjectMembersHandler = getProjectMembersHandler;
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

    [HttpPut("{projectId:guid}")]
    [ProducesResponseType(
        typeof(UpdateProjectResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UpdateProjectResponse>> Update(
        Guid projectId,
        UpdateProjectRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateProjectCommand(
            ProjectId: projectId,
            Name: request.Name,
            Description: request.Description);

        var result =
            await _updateProjectHandler.HandleAsync(
                command,
                cancellationToken);

        var response = new UpdateProjectResponse(
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

    [HttpPost("{projectId:guid}/archive")]
    [ProducesResponseType(
        typeof(ProjectLifecycleResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectLifecycleResponse>> Archive(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var result =
            await _archiveProjectHandler.HandleAsync(
                new ArchiveProjectCommand(projectId),
                cancellationToken);

        return Ok(
            new ProjectLifecycleResponse(
                ProjectId: result.ProjectId,
                OwnerId: result.OwnerId,
                Name: result.Name,
                Description: result.Description,
                IsArchived: result.IsArchived,
                CreatedAtUtc: result.CreatedAtUtc,
                UpdatedAtUtc: result.UpdatedAtUtc,
                ArchivedAtUtc: result.ArchivedAtUtc));
    }

    [HttpPost("{projectId:guid}/restore")]
    [ProducesResponseType(
        typeof(ProjectLifecycleResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectLifecycleResponse>> Restore(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var result =
            await _restoreProjectHandler.HandleAsync(
                new RestoreProjectCommand(projectId),
                cancellationToken);

        return Ok(
            new ProjectLifecycleResponse(
                ProjectId: result.ProjectId,
                OwnerId: result.OwnerId,
                Name: result.Name,
                Description: result.Description,
                IsArchived: result.IsArchived,
                CreatedAtUtc: result.CreatedAtUtc,
                UpdatedAtUtc: result.UpdatedAtUtc,
                ArchivedAtUtc: result.ArchivedAtUtc));
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<GetProjectResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<GetProjectResponse>>> GetAll(
        [FromQuery] PaginationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _getProjectsHandler.HandleAsync(
            new GetProjectsQuery(request.Page, request.PageSize),
            cancellationToken);

        var items = result.Items.Select(project => new GetProjectResponse(
            project.ProjectId,
            project.OwnerId,
            project.Name,
            project.Description,
            project.IsArchived,
            project.CreatedAtUtc,
            project.UpdatedAtUtc,
            project.ArchivedAtUtc)).ToArray();

        return Ok(new PagedResponse<GetProjectResponse>(
            items, result.Page, result.PageSize, result.TotalCount, result.TotalPages));
    }

    [HttpPost("{projectId:guid}/members")]
    [ProducesResponseType(
        typeof(AddProjectMemberResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
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

    [HttpGet("{projectId:guid}/members")]
    [ProducesResponseType(typeof(PagedResponse<GetProjectMemberResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResponse<GetProjectMemberResponse>>> GetMembers(
        Guid projectId,
        [FromQuery] PaginationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _getProjectMembersHandler.HandleAsync(
            new GetProjectMembersQuery(projectId, request.Page, request.PageSize),
            cancellationToken);

        var items = result.Items.Select(member => new GetProjectMemberResponse(
            member.ProjectMemberId,
            member.UserId,
            member.Role,
            member.JoinedAtUtc,
            member.UpdatedAtUtc)).ToArray();

        return Ok(new PagedResponse<GetProjectMemberResponse>(
            items, result.Page, result.PageSize, result.TotalCount, result.TotalPages));
    }

    [HttpPatch("{projectId:guid}/members/{userId:guid}/role")]
    [ProducesResponseType(
        typeof(ChangeProjectMemberRoleResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
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

    [HttpDelete("{projectId:guid}/members/{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
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
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
