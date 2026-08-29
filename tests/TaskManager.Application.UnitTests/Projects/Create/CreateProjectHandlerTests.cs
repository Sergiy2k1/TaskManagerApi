using NSubstitute;
using TaskManager.Application.Abstractions.Authentication;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Abstractions.Time;
using TaskManager.Application.Projects.Create;
using TaskManager.Domain.Entities;
using Xunit;

namespace TaskManager.Application.UnitTests.Projects.Create;

public sealed class CreateProjectHandlerTests
{
    [Fact]
    public async Task HandleAsyncWithValidCommandCreatesAndPersistsProject()
    {
        var projectRepository =
            Substitute.For<IProjectRepository>();

        var unitOfWork =
            Substitute.For<IUnitOfWork>();

        var currentUser =
            Substitute.For<ICurrentUser>();

        var clock =
            Substitute.For<IClock>();

        var userId = Guid.NewGuid();

        var now = new DateTimeOffset(
            2026,
            8,
            29,
            12,
            0,
            0,
            TimeSpan.Zero);

        currentUser.UserId.Returns(userId);
        clock.UtcNow.Returns(now);

        unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(1);

        var handler = new CreateProjectHandler(
            projectRepository,
            unitOfWork,
            currentUser,
            clock);

        var command = new CreateProjectCommand(
            Name: "Task Manager",
            Description: "Senior backend pet project");

        var cancellationToken =
            TestContext.Current.CancellationToken;

        var result = await handler.HandleAsync(
            command,
            cancellationToken);

        Assert.Equal(userId, result.OwnerId);
        Assert.Equal(command.Name, result.Name);
        Assert.Equal(command.Description, result.Description);
        Assert.Equal(now, result.CreatedAtUtc);
        Assert.False(result.IsArchived);

        projectRepository
            .Received(1)
            .Add(
                Arg.Is<Project>(
                    project =>
                        project.Id == result.ProjectId &&
                        project.OwnerId == userId &&
                        project.Name == command.Name &&
                        project.Description == command.Description &&
                        project.CreatedAtUtc == now));

        await unitOfWork
            .Received(1)
            .SaveChangesAsync(cancellationToken);
    }
}