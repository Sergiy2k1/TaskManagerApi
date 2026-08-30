using NSubstitute;
using TaskManager.Application.Abstractions.Authentication;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Projects.GetById;
using TaskManager.Domain.Entities;
using Xunit;

namespace TaskManager.Application.UnitTests.Projects.GetById;

public sealed class GetProjectByIdHandlerTests
{
    [Fact]
    public async Task HandleAsyncWhenProjectExistsReturnsProject()
    {
        var projectRepository =
            Substitute.For<IProjectRepository>();

        var currentUser =
            Substitute.For<ICurrentUser>();

        var ownerId = Guid.NewGuid();

        var createdAtUtc = new DateTimeOffset(
            2026,
            8,
            30,
            0,
            0,
            0,
            TimeSpan.Zero);

        var project = Project.Create(
            ownerId: ownerId,
            name: "Task Manager",
            description: "Senior backend pet project",
            createdAtUtc: createdAtUtc);

        currentUser.UserId.Returns(ownerId);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        projectRepository
            .GetByIdAsync(
                project.Id,
                cancellationToken)
            .Returns(project);

        var handler = new GetProjectByIdHandler(
            projectRepository,
            currentUser);

        var query = new GetProjectByIdQuery(
            ProjectId: project.Id);

        var result = await handler.HandleAsync(
            query,
            cancellationToken);

        Assert.Equal(project.Id, result.ProjectId);
        Assert.Equal(project.OwnerId, result.OwnerId);
        Assert.Equal(project.Name, result.Name);
        Assert.Equal(project.Description, result.Description);
        Assert.Equal(project.IsArchived, result.IsArchived);
        Assert.Equal(project.CreatedAtUtc, result.CreatedAtUtc);
        Assert.Equal(project.UpdatedAtUtc, result.UpdatedAtUtc);
        Assert.Equal(project.ArchivedAtUtc, result.ArchivedAtUtc);

        await projectRepository
            .Received(1)
            .GetByIdAsync(
                project.Id,
                cancellationToken);
    }

    [Fact]
    public async Task HandleAsyncWhenProjectBelongsToAnotherUserThrowsNotFoundException()
    {
        var projectRepository =
            Substitute.For<IProjectRepository>();

        var currentUser =
            Substitute.For<ICurrentUser>();

        var ownerId = Guid.NewGuid();
        var anotherUserId = Guid.NewGuid();

        var createdAtUtc = new DateTimeOffset(
            2026,
            8,
            30,
            0,
            0,
            0,
            TimeSpan.Zero);

        var project = Project.Create(
            ownerId: ownerId,
            name: "Task Manager",
            description: null,
            createdAtUtc: createdAtUtc);

        currentUser.UserId.Returns(anotherUserId);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        projectRepository
            .GetByIdAsync(
                project.Id,
                cancellationToken)
            .Returns(project);

        var handler = new GetProjectByIdHandler(
            projectRepository,
            currentUser);

        var query = new GetProjectByIdQuery(
            ProjectId: project.Id);

        var exception =
            await Assert.ThrowsAsync<ApplicationNotFoundException>(
                () => handler.HandleAsync(
                    query,
                    cancellationToken));

        Assert.Equal(
            "Project was not found.",
            exception.Message);

        await projectRepository
            .Received(1)
            .GetByIdAsync(
                project.Id,
                cancellationToken);
    }
    [Fact]
    public async Task HandleAsyncWhenProjectIdIsEmptyThrowsValidationException()
    {
        var projectRepository =
            Substitute.For<IProjectRepository>();

        var currentUser =
            Substitute.For<ICurrentUser>();

        var handler = new GetProjectByIdHandler(
            projectRepository,
            currentUser);

        var query = new GetProjectByIdQuery(
            ProjectId: Guid.Empty);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        var exception =
            await Assert.ThrowsAsync<ApplicationValidationException>(
                () => handler.HandleAsync(
                    query,
                    cancellationToken));

        Assert.Equal(
            "Project identifier cannot be empty.",
            exception.Message);

        Assert.Equal(
            nameof(query.ProjectId),
            exception.ParameterName);

        await projectRepository
            .DidNotReceive()
            .GetByIdAsync(
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>());
    }
}