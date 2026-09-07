using TaskManager.Application.Abstractions.Authentication;
using TaskManager.Application.Abstractions.Messaging;
using TaskManager.Application.Abstractions.Persistence;

namespace TaskManager.Application.Projects.GetAll;

public sealed class GetProjectsHandler
    : IQueryHandler<
        GetProjectsQuery,
        IReadOnlyList<GetProjectsResult>>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ICurrentUser _currentUser;

    public GetProjectsHandler(
        IProjectRepository projectRepository,
        ICurrentUser currentUser)
    {
        _projectRepository = projectRepository;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<GetProjectsResult>> HandleAsync(
        GetProjectsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var projects =
            await _projectRepository.GetAccessibleByUserIdAsync(
                _currentUser.UserId,
                cancellationToken);

        return projects
            .Select(
                project =>
                    new GetProjectsResult(
                        ProjectId: project.Id,
                        OwnerId: project.OwnerId,
                        Name: project.Name,
                        Description: project.Description,
                        IsArchived: project.IsArchived,
                        CreatedAtUtc: project.CreatedAtUtc,
                        UpdatedAtUtc: project.UpdatedAtUtc,
                        ArchivedAtUtc: project.ArchivedAtUtc))
            .ToList();
    }
}
