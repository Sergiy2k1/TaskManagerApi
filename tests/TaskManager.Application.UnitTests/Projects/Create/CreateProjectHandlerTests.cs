using NSubstitute;
using TaskManager.Application.Abstractions.Authentication;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Abstractions.Time;
using TaskManager.Application.Projects.Create;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using Xunit;

namespace TaskManager.Application.UnitTests.Projects.Create;

public sealed class CreateProjectHandlerTests
{
    [Fact]
    public async Task HandleAsyncWithValidCommandCreatesProjectAndOwnerMembership()
    {
        var projectRepository =
            Substitute.For<IProjectRepository>();

        var projectMemberRepository =
            Substitute.For<IProjectMemberRepository>();

        var unitOfWork =
            Substitute.For<IUnitOfWork>();

        var currentUser =
            Substitute.For<ICurrentUser>();

        var clock =
            Substitute.For<IClock>();

        var userId =
            Guid.NewGuid();

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
            .SaveChangesAsync(
                Arg.Any<CancellationToken>())
            .Returns(1);

        var handler = new CreateProjectHandler(
            projectRepository,
            projectMemberRepository,
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

        projectMemberRepository
            .Received(1)
            .Add(
                Arg.Is<ProjectMember>(
                    member =>
                        member.ProjectId == result.ProjectId &&
                        member.UserId == userId &&
                        member.Role == ProjectMemberRole.Manager &&
                        member.JoinedAtUtc == now &&
                        member.IsActive));

        await unitOfWork
            .Received(1)
            .SaveChangesAsync(
                cancellationToken);
    }
}