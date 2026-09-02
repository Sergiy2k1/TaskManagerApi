using NSubstitute;
using TaskManager.Application.Abstractions.Authentication;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Projects.GetMembers;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using Xunit;

namespace TaskManager.Application.UnitTests.Projects.GetMembers;

public sealed class GetProjectMembersHandlerTests
{
    [Fact]
    public async Task HandleAsyncWhenCurrentUserIsOwnerReturnsActiveMembers()
    {
        var projectRepository =
            Substitute.For<IProjectRepository>();

        var projectMemberRepository =
            Substitute.For<IProjectMemberRepository>();

        var currentUser =
            Substitute.For<ICurrentUser>();

        var createdAtUtc =
            CreateUtcTime();

        var ownerId =
            Guid.NewGuid();

        var memberUserId =
            Guid.NewGuid();

        var project =
            Project.Create(
                ownerId,
                "Task Manager",
                null,
                createdAtUtc);

        var ownerMembership =
            ProjectMember.Create(
                project.Id,
                ownerId,
                ProjectMemberRole.Manager,
                createdAtUtc);

        var memberMembership =
            ProjectMember.Create(
                project.Id,
                memberUserId,
                ProjectMemberRole.Member,
                createdAtUtc.AddMinutes(1));

        IReadOnlyList<ProjectMember> members =
        [
            ownerMembership,
            memberMembership
        ];

        currentUser.UserId.Returns(ownerId);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        projectRepository
            .GetByIdAsync(
                project.Id,
                cancellationToken)
            .Returns(project);

        projectMemberRepository
            .GetActiveByProjectAsync(
                project.Id,
                cancellationToken)
            .Returns(members);

        var handler =
            CreateHandler(
                projectRepository,
                projectMemberRepository,
                currentUser);

        var query =
            new GetProjectMembersQuery(
                project.Id);

        var result =
            await handler.HandleAsync(
                query,
                cancellationToken);

        Assert.Equal(
            2,
            result.Count);

        Assert.Equal(
            ownerMembership.Id,
            result[0].ProjectMemberId);

        Assert.Equal(
            ownerId,
            result[0].UserId);

        Assert.Equal(
            ProjectMemberRole.Manager,
            result[0].Role);

        Assert.Equal(
            memberMembership.Id,
            result[1].ProjectMemberId);

        Assert.Equal(
            memberUserId,
            result[1].UserId);

        Assert.Equal(
            ProjectMemberRole.Member,
            result[1].Role);

        await projectMemberRepository
            .DidNotReceive()
            .GetByProjectAndUserAsync(
                project.Id,
                ownerId,
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsyncWhenCurrentUserIsActiveMemberReturnsMembers()
    {
        var projectRepository =
            Substitute.For<IProjectRepository>();

        var projectMemberRepository =
            Substitute.For<IProjectMemberRepository>();

        var currentUser =
            Substitute.For<ICurrentUser>();

        var createdAtUtc =
            CreateUtcTime();

        var ownerId =
            Guid.NewGuid();

        var currentUserId =
            Guid.NewGuid();

        var project =
            Project.Create(
                ownerId,
                "Task Manager",
                null,
                createdAtUtc);

        var currentMembership =
            ProjectMember.Create(
                project.Id,
                currentUserId,
                ProjectMemberRole.Member,
                createdAtUtc);

        IReadOnlyList<ProjectMember> members =
        [
            currentMembership
        ];

        currentUser.UserId.Returns(currentUserId);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        projectRepository
            .GetByIdAsync(
                project.Id,
                cancellationToken)
            .Returns(project);

        projectMemberRepository
            .GetByProjectAndUserAsync(
                project.Id,
                currentUserId,
                cancellationToken)
            .Returns(currentMembership);

        projectMemberRepository
            .GetActiveByProjectAsync(
                project.Id,
                cancellationToken)
            .Returns(members);

        var handler =
            CreateHandler(
                projectRepository,
                projectMemberRepository,
                currentUser);

        var query =
            new GetProjectMembersQuery(
                project.Id);

        var result =
            await handler.HandleAsync(
                query,
                cancellationToken);

        Assert.Single(result);

        Assert.Equal(
            currentUserId,
            result[0].UserId);

        await projectMemberRepository
            .Received(1)
            .GetActiveByProjectAsync(
                project.Id,
                cancellationToken);
    }

    [Fact]
    public async Task HandleAsyncWhenCurrentUserIsRemovedMemberThrowsNotFoundException()
    {
        var projectRepository =
            Substitute.For<IProjectRepository>();

        var projectMemberRepository =
            Substitute.For<IProjectMemberRepository>();

        var currentUser =
            Substitute.For<ICurrentUser>();

        var createdAtUtc =
            CreateUtcTime();

        var ownerId =
            Guid.NewGuid();

        var currentUserId =
            Guid.NewGuid();

        var project =
            Project.Create(
                ownerId,
                "Task Manager",
                null,
                createdAtUtc);

        var removedMembership =
            ProjectMember.Create(
                project.Id,
                currentUserId,
                ProjectMemberRole.Member,
                createdAtUtc);

        removedMembership.Remove(
            createdAtUtc.AddMinutes(1));

        currentUser.UserId.Returns(currentUserId);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        projectRepository
            .GetByIdAsync(
                project.Id,
                cancellationToken)
            .Returns(project);

        projectMemberRepository
            .GetByProjectAndUserAsync(
                project.Id,
                currentUserId,
                cancellationToken)
            .Returns(removedMembership);

        var handler =
            CreateHandler(
                projectRepository,
                projectMemberRepository,
                currentUser);

        var query =
            new GetProjectMembersQuery(
                project.Id);

        var exception =
            await Assert.ThrowsAsync<ApplicationNotFoundException>(
                () => handler.HandleAsync(
                    query,
                    cancellationToken));

        Assert.Equal(
            "Project was not found.",
            exception.Message);

        await projectMemberRepository
            .DidNotReceive()
            .GetActiveByProjectAsync(
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsyncWhenCurrentUserIsNotMemberThrowsNotFoundException()
    {
        var projectRepository =
            Substitute.For<IProjectRepository>();

        var projectMemberRepository =
            Substitute.For<IProjectMemberRepository>();

        var currentUser =
            Substitute.For<ICurrentUser>();

        var createdAtUtc =
            CreateUtcTime();

        var ownerId =
            Guid.NewGuid();

        var currentUserId =
            Guid.NewGuid();

        var project =
            Project.Create(
                ownerId,
                "Task Manager",
                null,
                createdAtUtc);

        currentUser.UserId.Returns(currentUserId);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        projectRepository
            .GetByIdAsync(
                project.Id,
                cancellationToken)
            .Returns(project);

        projectMemberRepository
            .GetByProjectAndUserAsync(
                project.Id,
                currentUserId,
                cancellationToken)
            .Returns((ProjectMember?)null);

        var handler =
            CreateHandler(
                projectRepository,
                projectMemberRepository,
                currentUser);

        var query =
            new GetProjectMembersQuery(
                project.Id);

        var exception =
            await Assert.ThrowsAsync<ApplicationNotFoundException>(
                () => handler.HandleAsync(
                    query,
                    cancellationToken));

        Assert.Equal(
            "Project was not found.",
            exception.Message);

        await projectMemberRepository
            .DidNotReceive()
            .GetActiveByProjectAsync(
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsyncWhenProjectDoesNotExistThrowsNotFoundException()
    {
        var projectRepository =
            Substitute.For<IProjectRepository>();

        var projectMemberRepository =
            Substitute.For<IProjectMemberRepository>();

        var currentUser =
            Substitute.For<ICurrentUser>();

        var projectId =
            Guid.NewGuid();

        var cancellationToken =
            TestContext.Current.CancellationToken;

        projectRepository
            .GetByIdAsync(
                projectId,
                cancellationToken)
            .Returns((Project?)null);

        var handler =
            CreateHandler(
                projectRepository,
                projectMemberRepository,
                currentUser);

        var query =
            new GetProjectMembersQuery(
                projectId);

        var exception =
            await Assert.ThrowsAsync<ApplicationNotFoundException>(
                () => handler.HandleAsync(
                    query,
                    cancellationToken));

        Assert.Equal(
            "Project was not found.",
            exception.Message);

        await projectMemberRepository
            .DidNotReceive()
            .GetActiveByProjectAsync(
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>());
    }

    private static GetProjectMembersHandler CreateHandler(
        IProjectRepository projectRepository,
        IProjectMemberRepository projectMemberRepository,
        ICurrentUser currentUser)
    {
        return new GetProjectMembersHandler(
            projectRepository,
            projectMemberRepository,
            currentUser);
    }

    private static DateTimeOffset CreateUtcTime()
    {
        return new DateTimeOffset(
            2026,
            9,
            2,
            19,
            0,
            0,
            TimeSpan.Zero);
    }
}