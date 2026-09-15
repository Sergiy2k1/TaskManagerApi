using NSubstitute;
using TaskManager.Application.Abstractions.Authentication;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Common.Pagination;
using TaskManager.Application.Common.Exceptions;
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
            .GetAccessiblePageByUserIdAsync(
                userId,
                1,
                20,
                cancellationToken)
            .Returns(new PagedResult<Project>(projects, 1, 20, projects.Count));

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
            .GetAccessiblePageByUserIdAsync(
                userId,
                1,
                20,
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
            .GetAccessiblePageByUserIdAsync(
                userId,
                1,
                20,
                cancellationToken)
            .Returns(new PagedResult<Project>(Array.Empty<Project>(), 1, 20, 0));

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

    [Theory]
    [InlineData(0, 20, "Page must be greater than or equal to 1.")]
    [InlineData(1, 0, "Page size must be between 1 and 100.")]
    [InlineData(1, 101, "Page size must be between 1 and 100.")]
    public async Task HandleAsyncWhenPaginationIsInvalidThrowsValidationException(int page, int pageSize, string expectedMessage)
    {
        var handler = new GetProjectsHandler(Substitute.For<IProjectRepository>(), Substitute.For<ICurrentUser>());
        var exception = await Assert.ThrowsAsync<ApplicationValidationException>(() => handler.HandleAsync(new GetProjectsQuery(page, pageSize), TestContext.Current.CancellationToken));
        Assert.Equal(expectedMessage, exception.Message);
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
