using TaskManager.Application.Abstractions.Authentication;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Abstractions.Time;
using TaskManager.Domain.Entities;

namespace TaskManager.Application.Projects.Create;

public sealed class CreateProjectHandler
{
    private readonly IProjectRepository _projectRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public CreateProjectHandler(
        IProjectRepository projectRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IClock clock)
    {
        _projectRepository = projectRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<CreateProjectResult> HandleAsync(
        CreateProjectCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var project = Project.Create(
            ownerId: _currentUser.UserId,
            name: command.Name,
            description: command.Description,
            createdAtUtc: _clock.UtcNow);

        _projectRepository.Add(project);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return new CreateProjectResult(
            ProjectId: project.Id,
            OwnerId: project.OwnerId,
            Name: project.Name,
            Description: project.Description,
            IsArchived: project.IsArchived,
            CreatedAtUtc: project.CreatedAtUtc);
    }
}