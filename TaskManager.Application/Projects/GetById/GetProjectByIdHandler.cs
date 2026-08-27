using TaskManager.Application.Abstractions.Authentication;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Common.Exceptions;

namespace TaskManager.Application.Projects.GetById;

public sealed class GetProjectByIdHandler
{
    private readonly IProjectRepository _projectRepository;
    private readonly ICurrentUser _currentUser;

    public GetProjectByIdHandler(
        IProjectRepository projectRepository,
        ICurrentUser currentUser)
    {
        _projectRepository = projectRepository;
        _currentUser = currentUser;
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

        if (project is null ||
            project.OwnerId != _currentUser.UserId)
        {
            throw new ApplicationNotFoundException(
                "Project was not found.");
        }

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