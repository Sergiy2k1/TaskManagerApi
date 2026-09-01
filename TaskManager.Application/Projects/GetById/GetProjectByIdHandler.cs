using TaskManager.Application.Abstractions.Authentication;
using TaskManager.Application.Abstractions.Messaging;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Common.Exceptions;

namespace TaskManager.Application.Projects.GetById;

public sealed class GetProjectByIdHandler
    : IQueryHandler<GetProjectByIdQuery, GetProjectByIdResult>
{
    private readonly IProjectRepository _projectRepository;
    private readonly IProjectMemberRepository _projectMemberRepository;
    private readonly ICurrentUser _currentUser;

    public GetProjectByIdHandler(
        IProjectRepository projectRepository,
        IProjectMemberRepository projectMemberRepository,
        ICurrentUser currentUser)
    {
        _projectRepository = projectRepository;
        _projectMemberRepository = projectMemberRepository;
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

        if (project is null)
        {
            throw new ApplicationNotFoundException(
                "Project was not found.");
        }

        if (project.OwnerId != _currentUser.UserId)
        {
            var projectMember =
                await _projectMemberRepository
                    .GetByProjectAndUserAsync(
                        project.Id,
                        _currentUser.UserId,
                        cancellationToken);

            if (projectMember is null ||
                !projectMember.IsActive)
            {
                throw new ApplicationNotFoundException(
                    "Project was not found.");
            }
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