using NSubstitute;
using TaskManager.Application.Abstractions.Authentication;
using TaskManager.Application.Abstractions.Authorization;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Abstractions.Time;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Projects.Archive;
using TaskManager.Application.Projects.Restore;
using TaskManager.Domain.Entities;
using Xunit;

namespace TaskManager.Application.UnitTests.Projects.Lifecycle;

public sealed class ProjectLifecycleHandlerTests
{
    [Fact]
    public async Task ArchiveOwnerArchivesProject()
    {
        var repo = Substitute.For<IProjectRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        var policy = Substitute.For<IProjectAccessPolicy>();
        var currentUser = Substitute.For<ICurrentUser>();
        var clock = Substitute.For<IClock>();

        var ownerId = Guid.NewGuid();
        var createdAtUtc = CreateUtcTime();
        var archivedAtUtc = createdAtUtc.AddMinutes(5);
        var project = Project.Create(ownerId, "Lifecycle", null, createdAtUtc);
        var token = TestContext.Current.CancellationToken;

        repo.GetByIdForUpdateAsync(project.Id, token).Returns(project);
        currentUser.UserId.Returns(ownerId);
        clock.UtcNow.Returns(archivedAtUtc);
        uow.SaveChangesAsync(token).Returns(1);

        var handler = new ArchiveProjectHandler(repo, uow, policy, currentUser, clock);
        var result = await handler.HandleAsync(new ArchiveProjectCommand(project.Id), token);

        Assert.True(result.IsArchived);
        Assert.Equal(archivedAtUtc, result.ArchivedAtUtc);
        Assert.Equal(archivedAtUtc, result.UpdatedAtUtc);
        await uow.Received(1).SaveChangesAsync(token);
    }

    [Fact]
    public async Task RestoreOwnerRestoresProject()
    {
        var repo = Substitute.For<IProjectRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        var policy = Substitute.For<IProjectAccessPolicy>();
        var currentUser = Substitute.For<ICurrentUser>();
        var clock = Substitute.For<IClock>();

        var ownerId = Guid.NewGuid();
        var createdAtUtc = CreateUtcTime();
        var project = Project.Create(ownerId, "Lifecycle", null, createdAtUtc);
        project.Archive(createdAtUtc.AddMinutes(5));
        var restoredAtUtc = createdAtUtc.AddMinutes(10);
        var token = TestContext.Current.CancellationToken;

        repo.GetByIdForUpdateAsync(project.Id, token).Returns(project);
        currentUser.UserId.Returns(ownerId);
        clock.UtcNow.Returns(restoredAtUtc);
        uow.SaveChangesAsync(token).Returns(1);

        var handler = new RestoreProjectHandler(repo, uow, policy, currentUser, clock);
        var result = await handler.HandleAsync(new RestoreProjectCommand(project.Id), token);

        Assert.False(result.IsArchived);
        Assert.Null(result.ArchivedAtUtc);
        Assert.Equal(restoredAtUtc, result.UpdatedAtUtc);
        await uow.Received(1).SaveChangesAsync(token);
    }

    [Fact]
    public async Task ArchiveNonOwnerThrowsForbidden()
    {
        var repo = Substitute.For<IProjectRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        var policy = Substitute.For<IProjectAccessPolicy>();
        var currentUser = Substitute.For<ICurrentUser>();
        var clock = Substitute.For<IClock>();

        var ownerId = Guid.NewGuid();
        var project = Project.Create(ownerId, "Lifecycle", null, CreateUtcTime());
        var token = TestContext.Current.CancellationToken;

        repo.GetByIdForUpdateAsync(project.Id, token).Returns(project);
        currentUser.UserId.Returns(Guid.NewGuid());

        var handler = new ArchiveProjectHandler(repo, uow, policy, currentUser, clock);

        var exception = await Assert.ThrowsAsync<ApplicationForbiddenException>(
            () => handler.HandleAsync(new ArchiveProjectCommand(project.Id), token));

        Assert.Equal("Only the project owner can archive the project.", exception.Message);
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RestoreNonOwnerThrowsForbidden()
    {
        var repo = Substitute.For<IProjectRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        var policy = Substitute.For<IProjectAccessPolicy>();
        var currentUser = Substitute.For<ICurrentUser>();
        var clock = Substitute.For<IClock>();

        var ownerId = Guid.NewGuid();
        var createdAtUtc = CreateUtcTime();
        var project = Project.Create(ownerId, "Lifecycle", null, createdAtUtc);
        project.Archive(createdAtUtc.AddMinutes(1));
        var token = TestContext.Current.CancellationToken;

        repo.GetByIdForUpdateAsync(project.Id, token).Returns(project);
        currentUser.UserId.Returns(Guid.NewGuid());

        var handler = new RestoreProjectHandler(repo, uow, policy, currentUser, clock);

        var exception = await Assert.ThrowsAsync<ApplicationForbiddenException>(
            () => handler.HandleAsync(new RestoreProjectCommand(project.Id), token));

        Assert.Equal("Only the project owner can restore the project.", exception.Message);
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static DateTimeOffset CreateUtcTime() =>
        new(2026, 9, 12, 12, 0, 0, TimeSpan.Zero);
}
