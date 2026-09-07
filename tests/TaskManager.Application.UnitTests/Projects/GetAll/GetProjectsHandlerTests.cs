using NSubstitute;
using TaskManager.Application.Abstractions.Authentication;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Projects.GetAll;
using TaskManager.Domain.Entities;
using Xunit;

namespace TaskManager.Application.UnitTests.Projects.GetAll;

public sealed class GetProjectsHandlerTests
{
    [Fact]
    public async Task HandleAsyncReturnsProjectsAccessibleToCurrentUser()
    {
        var projectRepository =
            Substitute.For<IProjectRepository>();

        var currentUser =
            Substitute.For<ICurrentUser>();

        var userId =
            Guid.NewGuid();

        var otherOwnerId =
            Guid.NewGuid();

        var createdAtUtc =
            new DateTimeOffset(
                2026,
                9,
                7,
                12,
                0,
                0,
                TimeSpan.Zero);

        var ownedProject =
            Project.Create(
                userId,
                "Owned Project",
                "Owned by current user",
                createdAtUtc);

        var memberProject =
            Project.Create(
                otherOwnerId,
                "Member Project",
                null,
                createdAtUtc.AddMinutes(1));

        IReadOnlyList<Project> projects =
        [
            ownedProject,
            memberProject
        ];

        currentUser.UserId.Returns(userId);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        projectRepository
            .GetAccessibleByUserIdAsync(
                userId,
                cancellationToken)
            .Returns(projects);

        var handler =
            new GetProjectsHandler(
                projectRepository,
                currentUser);

        var result =
            await handler.HandleAsync(
                new GetProjectsQuery(),
                cancellationToken);

        Assert.Equal(
            2,
            result.Count);

        Assert.Equal(
            ownedProject.Id,
            result[0].ProjectId);

        Assert.Equal(
            ownedProject.OwnerId,
            result[0].OwnerId);

        Assert.Equal(
            ownedProject.Name,
            result[0].Name);

        Assert.Equal(
            memberProject.Id,
            result[1].ProjectId);

        await projectRepository
            .Received(1)
            .GetAccessibleByUserIdAsync(
                userId,
                cancellationToken);
    }

    [Fact]
    public async Task HandleAsyncWhenNoProjectsAreAccessibleReturnsEmptyList()
    {
        var projectRepository =
            Substitute.For<IProjectRepository>();

        var currentUser =
            Substitute.For<ICurrentUser>();

        var userId =
            Guid.NewGuid();

        currentUser.UserId.Returns(userId);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        projectRepository
            .GetAccessibleByUserIdAsync(
                userId,
                cancellationToken)
            .Returns(Array.Empty<Project>());

        var handler =
            new GetProjectsHandler(
                projectRepository,
                currentUser);

        var result =
            await handler.HandleAsync(
                new GetProjectsQuery(),
                cancellationToken);

        Assert.Empty(result);
    }

    [Fact]
    public async Task HandleAsyncWhenQueryIsNullThrowsArgumentNullException()
    {
        var projectRepository =
            Substitute.For<IProjectRepository>();

        var currentUser =
            Substitute.For<ICurrentUser>();

        var handler =
            new GetProjectsHandler(
                projectRepository,
                currentUser);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.HandleAsync(
                null!,
                cancellationToken));
    }
}
