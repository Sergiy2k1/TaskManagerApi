using NSubstitute;
using TaskManager.Application.Abstractions.Authentication;
using TaskManager.Application.Abstractions.Authorization;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Abstractions.Time;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Projects.Update;
using TaskManager.Domain.Entities;
using Xunit;

namespace TaskManager.Application.UnitTests.Projects.Update;

public sealed class UpdateProjectHandlerTests
{
    [Fact]
    public async Task HandleAsyncWhenCurrentUserIsOwnerUpdatesProject()
    {
        var projectRepository = Substitute.For<IProjectRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var projectAccessPolicy = Substitute.For<IProjectAccessPolicy>();
        var currentUser = Substitute.For<ICurrentUser>();
        var clock = Substitute.For<IClock>();

        var ownerId = Guid.NewGuid();
        var createdAtUtc = CreateUtcTime();
        var changedAtUtc = createdAtUtc.AddHours(1);

        var project = Project.Create(
            ownerId,
            "Old Project",
            "Old description",
            createdAtUtc);

        currentUser.UserId.Returns(ownerId);
        clock.UtcNow.Returns(changedAtUtc);

        var cancellationToken = TestContext.Current.CancellationToken;

        projectRepository
            .GetByIdForUpdateAsync(project.Id, cancellationToken)
            .Returns(project);

        unitOfWork
            .SaveChangesAsync(cancellationToken)
            .Returns(1);

        var handler = CreateHandler(
            projectRepository,
            unitOfWork,
            projectAccessPolicy,
            currentUser,
            clock);

        var result = await handler.HandleAsync(
            new UpdateProjectCommand(
                project.Id,
                "Updated Project",
                "Updated description"),
            cancellationToken);

        Assert.Equal(project.Id, result.ProjectId);
        Assert.Equal(ownerId, result.OwnerId);
        Assert.Equal("Updated Project", result.Name);
        Assert.Equal("Updated description", result.Description);
        Assert.False(result.IsArchived);
        Assert.Equal(createdAtUtc, result.CreatedAtUtc);
        Assert.Equal(changedAtUtc, result.UpdatedAtUtc);
        Assert.Null(result.ArchivedAtUtc);

        await projectAccessPolicy
            .Received(1)
            .EnsureHasAccessAsync(
                ownerId,
                project.Id,
                cancellationToken);

        await unitOfWork
            .Received(1)
            .SaveChangesAsync(cancellationToken);
    }

    [Fact]
    public async Task HandleAsyncWhenActiveMemberIsNotOwnerThrowsForbiddenException()
    {
        var projectRepository = Substitute.For<IProjectRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var projectAccessPolicy = Substitute.For<IProjectAccessPolicy>();
        var currentUser = Substitute.For<ICurrentUser>();
        var clock = Substitute.For<IClock>();

        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var project = Project.Create(
            ownerId,
            "Owner Only Project",
            null,
            CreateUtcTime());

        currentUser.UserId.Returns(memberId);

        var cancellationToken = TestContext.Current.CancellationToken;

        projectRepository
            .GetByIdForUpdateAsync(project.Id, cancellationToken)
            .Returns(project);

        var handler = CreateHandler(
            projectRepository,
            unitOfWork,
            projectAccessPolicy,
            currentUser,
            clock);

        var exception = await Assert.ThrowsAsync<ApplicationForbiddenException>(
            () => handler.HandleAsync(
                new UpdateProjectCommand(
                    project.Id,
                    "Changed",
                    null),
                cancellationToken));

        Assert.Equal(
            "Only the project owner can update the project.",
            exception.Message);

        await unitOfWork
            .DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsyncWhenProjectIsArchivedThrowsConflictException()
    {
        var projectRepository = Substitute.For<IProjectRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var projectAccessPolicy = Substitute.For<IProjectAccessPolicy>();
        var currentUser = Substitute.For<ICurrentUser>();
        var clock = Substitute.For<IClock>();

        var ownerId = Guid.NewGuid();
        var createdAtUtc = CreateUtcTime();
        var project = Project.Create(
            ownerId,
            "Archived Project",
            null,
            createdAtUtc);

        project.Archive(createdAtUtc.AddMinutes(1));
        currentUser.UserId.Returns(ownerId);

        var cancellationToken = TestContext.Current.CancellationToken;

        projectRepository
            .GetByIdForUpdateAsync(project.Id, cancellationToken)
            .Returns(project);

        var handler = CreateHandler(
            projectRepository,
            unitOfWork,
            projectAccessPolicy,
            currentUser,
            clock);

        var exception = await Assert.ThrowsAsync<ApplicationConflictException>(
            () => handler.HandleAsync(
                new UpdateProjectCommand(
                    project.Id,
                    "Changed",
                    null),
                cancellationToken));

        Assert.Equal(
            "Archived project cannot be updated.",
            exception.Message);

        await unitOfWork
            .DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsyncWhenProjectDoesNotExistThrowsNotFoundException()
    {
        var projectRepository = Substitute.For<IProjectRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var projectAccessPolicy = Substitute.For<IProjectAccessPolicy>();
        var currentUser = Substitute.For<ICurrentUser>();
        var clock = Substitute.For<IClock>();

        var projectId = Guid.NewGuid();
        var cancellationToken = TestContext.Current.CancellationToken;

        projectRepository
            .GetByIdForUpdateAsync(projectId, cancellationToken)
            .Returns((Project?)null);

        var handler = CreateHandler(
            projectRepository,
            unitOfWork,
            projectAccessPolicy,
            currentUser,
            clock);

        var exception = await Assert.ThrowsAsync<ApplicationNotFoundException>(
            () => handler.HandleAsync(
                new UpdateProjectCommand(
                    projectId,
                    "Changed",
                    null),
                cancellationToken));

        Assert.Equal("Project was not found.", exception.Message);

        await projectAccessPolicy
            .DidNotReceive()
            .EnsureHasAccessAsync(
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsyncWhenProjectIdIsEmptyThrowsValidationException()
    {
        var handler = CreateHandler(
            Substitute.For<IProjectRepository>(),
            Substitute.For<IUnitOfWork>(),
            Substitute.For<IProjectAccessPolicy>(),
            Substitute.For<ICurrentUser>(),
            Substitute.For<IClock>());

        var cancellationToken = TestContext.Current.CancellationToken;

        var exception = await Assert.ThrowsAsync<ApplicationValidationException>(
            () => handler.HandleAsync(
                new UpdateProjectCommand(
                    Guid.Empty,
                    "Changed",
                    null),
                cancellationToken));

        Assert.Equal(
            "Project identifier cannot be empty.",
            exception.Message);
    }

    [Fact]
    public async Task HandleAsyncWhenCommandIsNullThrowsArgumentNullException()
    {
        var handler = CreateHandler(
            Substitute.For<IProjectRepository>(),
            Substitute.For<IUnitOfWork>(),
            Substitute.For<IProjectAccessPolicy>(),
            Substitute.For<ICurrentUser>(),
            Substitute.For<IClock>());

        var cancellationToken = TestContext.Current.CancellationToken;

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.HandleAsync(
                null!,
                cancellationToken));
    }

    private static UpdateProjectHandler CreateHandler(
        IProjectRepository projectRepository,
        IUnitOfWork unitOfWork,
        IProjectAccessPolicy projectAccessPolicy,
        ICurrentUser currentUser,
        IClock clock)
    {
        return new UpdateProjectHandler(
            projectRepository,
            unitOfWork,
            projectAccessPolicy,
            currentUser,
            clock);
    }

    private static DateTimeOffset CreateUtcTime()
    {
        return new DateTimeOffset(
            2026,
            9,
            7,
            12,
            0,
            0,
            TimeSpan.Zero);
    }
}
