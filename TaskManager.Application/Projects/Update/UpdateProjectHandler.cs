using TaskManager.Application.Abstractions.Authentication;
using TaskManager.Application.Abstractions.Authorization;
using TaskManager.Application.Abstractions.Messaging;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Abstractions.Time;
using TaskManager.Application.Common.Exceptions;

namespace TaskManager.Application.Projects.Update;

public sealed class UpdateProjectHandler
    : ICommandHandler<UpdateProjectCommand, UpdateProjectResult>
{
    private readonly IProjectRepository _projectRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProjectAccessPolicy _projectAccessPolicy;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public UpdateProjectHandler(
        IProjectRepository projectRepository,
        IUnitOfWork unitOfWork,
        IProjectAccessPolicy projectAccessPolicy,
        ICurrentUser currentUser,
        IClock clock)
    {
        _projectRepository = projectRepository;
        _unitOfWork = unitOfWork;
        _projectAccessPolicy = projectAccessPolicy;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<UpdateProjectResult> HandleAsync(
        UpdateProjectCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.ProjectId == Guid.Empty)
        {
            throw new ApplicationValidationException(
                "Project identifier cannot be empty.",
                nameof(command.ProjectId));
        }

        var project =
            await _projectRepository.GetByIdForUpdateAsync(
                command.ProjectId,
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

        if (project.OwnerId != _currentUser.UserId)
        {
            throw new ApplicationForbiddenException(
                "Only the project owner can update the project.");
        }

        var changedAtUtc =
            _clock.UtcNow;

        project.Rename(
            command.Name,
            changedAtUtc);

        project.ChangeDescription(
            command.Description,
            changedAtUtc);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return new UpdateProjectResult(
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
