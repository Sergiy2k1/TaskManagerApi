using TaskManager.Application.Abstractions.Authentication;
using TaskManager.Application.Abstractions.Messaging;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Abstractions.Time;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;

namespace TaskManager.Application.Projects.Create;

public sealed class CreateProjectHandler
    : ICommandHandler<CreateProjectCommand, CreateProjectResult>
{
    private readonly IProjectRepository _projectRepository;
    private readonly IProjectMemberRepository _projectMemberRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public CreateProjectHandler(
        IProjectRepository projectRepository,
        IProjectMemberRepository projectMemberRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IClock clock)
    {
        _projectRepository = projectRepository;
        _projectMemberRepository = projectMemberRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<CreateProjectResult> HandleAsync(
        CreateProjectCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var createdAtUtc =
            _clock.UtcNow;

        var project = Project.Create(
            ownerId: _currentUser.UserId,
            name: command.Name,
            description: command.Description,
            createdAtUtc: createdAtUtc);

        var ownerMember = ProjectMember.Create(
            projectId: project.Id,
            userId: _currentUser.UserId,
            role: ProjectMemberRole.Manager,
            joinedAtUtc: createdAtUtc);

        _projectRepository.Add(project);
        _projectMemberRepository.Add(ownerMember);

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