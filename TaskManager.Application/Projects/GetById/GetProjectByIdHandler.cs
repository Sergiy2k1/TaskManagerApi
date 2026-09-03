using TaskManager.Application.Abstractions.Authorization;
using TaskManager.Application.Abstractions.Messaging;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Common.Exceptions;

namespace TaskManager.Application.Projects.GetById;

public sealed class GetProjectByIdHandler
    : IQueryHandler<GetProjectByIdQuery, GetProjectByIdResult>
{
    private readonly IProjectRepository _projectRepository;
    private readonly IProjectAccessPolicy _projectAccessPolicy;

    public GetProjectByIdHandler(
        IProjectRepository projectRepository,
        IProjectAccessPolicy projectAccessPolicy)
    {
        _projectRepository = projectRepository;
        _projectAccessPolicy = projectAccessPolicy;
    }

    public async Task<GetProjectByIdResult> HandleAsync(
        GetProjectByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.ProjectId == Guid.Empty)
        {
            throw new ApplicationValidationException(
                "Project identifier cannot be empty.",
                nameof(query.ProjectId));
        }

        var project =
            await _projectRepository.GetByIdAsync(
                query.ProjectId,
                cancellationToken);

        if (project is null)
        {
            throw new ApplicationNotFoundException(
                "Project was not found.");
        }

        await _projectAccessPolicy.EnsureHasAccessAsync(
            project.OwnerId,
            project.Id,
            cancellationToken);

        return new GetProjectByIdResult(
            ProjectId: project.Id,
            OwnerId: project.OwnerId,
            Name: project.Name,
            Description: project.Description,
            IsArchived: project.IsArchived,
            CreatedAtUtc: project.CreatedAtUtc,
            UpdatedAtUtc: project.UpdatedAtUtc,
            ArchivedAtUtc: project.ArchivedAtUtc);
    }
}