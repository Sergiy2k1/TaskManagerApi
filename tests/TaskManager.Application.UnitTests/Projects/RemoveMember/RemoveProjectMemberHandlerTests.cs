using NSubstitute;
using TaskManager.Application.Abstractions.Authentication;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Abstractions.Time;
using TaskManager.Application.Common.Authorization;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Projects.RemoveMember;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using Xunit;

namespace TaskManager.Application.UnitTests.Projects.RemoveMember;

public sealed class RemoveProjectMemberHandlerTests
{
    [Fact]
    public async Task HandleAsyncWhenCurrentUserIsOwnerRemovesMember()
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

        var joinedAtUtc =
            CreateUtcTime();

        var removedAtUtc =
            joinedAtUtc.AddMinutes(1);

        var ownerId =
            Guid.NewGuid();

        var memberUserId =
            Guid.NewGuid();

        var project =
            Project.Create(
                ownerId,
                "Task Manager",
                null,
                joinedAtUtc);

        var membership =
            ProjectMember.Create(
                project.Id,
                memberUserId,
                ProjectMemberRole.Member,
                joinedAtUtc);

        currentUser.UserId.Returns(ownerId);
        clock.UtcNow.Returns(removedAtUtc);

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
                memberUserId,
                cancellationToken)
            .Returns(membership);

        unitOfWork
            .SaveChangesAsync(cancellationToken)
            .Returns(1);

        var handler = CreateHandler(
            projectRepository,
            projectMemberRepository,
            unitOfWork,
            currentUser,
            clock);

        var command =
            new RemoveProjectMemberCommand(
                ProjectId: project.Id,
                UserId: memberUserId);

        var result =
            await handler.HandleAsync(
                command,
                cancellationToken);

        Assert.Equal(
            membership.Id,
            result.ProjectMemberId);

        Assert.Equal(
            project.Id,
            result.ProjectId);

        Assert.Equal(
            memberUserId,
            result.UserId);

        Assert.Equal(
            removedAtUtc,
            result.RemovedAtUtc);

        Assert.False(
            membership.IsActive);

        Assert.Equal(
            removedAtUtc,
            membership.RemovedAtUtc);

        Assert.Equal(
            removedAtUtc,
            membership.UpdatedAtUtc);

        await unitOfWork
            .Received(1)
            .SaveChangesAsync(cancellationToken);
    }

    [Fact]
    public async Task HandleAsyncWhenCurrentUserIsManagerRemovesMember()
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

        var joinedAtUtc =
            CreateUtcTime();

        var removedAtUtc =
            joinedAtUtc.AddMinutes(1);

        var ownerId =
            Guid.NewGuid();

        var managerUserId =
            Guid.NewGuid();

        var targetUserId =
            Guid.NewGuid();

        var project =
            Project.Create(
                ownerId,
                "Task Manager",
                null,
                joinedAtUtc);

        var managerMembership =
            ProjectMember.Create(
                project.Id,
                managerUserId,
                ProjectMemberRole.Manager,
                joinedAtUtc);

        var targetMembership =
            ProjectMember.Create(
                project.Id,
                targetUserId,
                ProjectMemberRole.Member,
                joinedAtUtc);

        currentUser.UserId.Returns(managerUserId);
        clock.UtcNow.Returns(removedAtUtc);

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
                managerUserId,
                cancellationToken)
            .Returns(managerMembership);

        projectMemberRepository
            .GetByProjectAndUserAsync(
                project.Id,
                targetUserId,
                cancellationToken)
            .Returns(targetMembership);

        unitOfWork
            .SaveChangesAsync(cancellationToken)
            .Returns(1);

        var handler = CreateHandler(
            projectRepository,
            projectMemberRepository,
            unitOfWork,
            currentUser,
            clock);

        var command =
            new RemoveProjectMemberCommand(
                ProjectId: project.Id,
                UserId: targetUserId);

        var result =
            await handler.HandleAsync(
                command,
                cancellationToken);

        Assert.Equal(
            removedAtUtc,
            result.RemovedAtUtc);

        Assert.False(
            targetMembership.IsActive);

        await unitOfWork
            .Received(1)
            .SaveChangesAsync(cancellationToken);
    }

    [Fact]
    public async Task HandleAsyncWhenCurrentUserIsMemberThrowsForbiddenException()
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

        var joinedAtUtc =
            CreateUtcTime();

        var ownerId =
            Guid.NewGuid();

        var currentUserId =
            Guid.NewGuid();

        var targetUserId =
            Guid.NewGuid();

        var project =
            Project.Create(
                ownerId,
                "Task Manager",
                null,
                joinedAtUtc);

        var currentMembership =
            ProjectMember.Create(
                project.Id,
                currentUserId,
                ProjectMemberRole.Member,
                joinedAtUtc);

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

        var handler = CreateHandler(
            projectRepository,
            projectMemberRepository,
            unitOfWork,
            currentUser,
            clock);

        var command =
            new RemoveProjectMemberCommand(
                ProjectId: project.Id,
                UserId: targetUserId);

        var exception =
            await Assert.ThrowsAsync<ApplicationForbiddenException>(
                () => handler.HandleAsync(
                    command,
                    cancellationToken));

        Assert.Equal(
            "You do not have permission to manage project members.",
            exception.Message);

        await unitOfWork
            .DidNotReceive()
            .SaveChangesAsync(
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsyncWhenCurrentUserIsNotMemberThrowsNotFoundException()
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

        var joinedAtUtc =
            CreateUtcTime();

        var ownerId =
            Guid.NewGuid();

        var currentUserId =
            Guid.NewGuid();

        var targetUserId =
            Guid.NewGuid();

        var project =
            Project.Create(
                ownerId,
                "Task Manager",
                null,
                joinedAtUtc);

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

        var handler = CreateHandler(
            projectRepository,
            projectMemberRepository,
            unitOfWork,
            currentUser,
            clock);

        var command =
            new RemoveProjectMemberCommand(
                ProjectId: project.Id,
                UserId: targetUserId);

        var exception =
            await Assert.ThrowsAsync<ApplicationNotFoundException>(
                () => handler.HandleAsync(
                    command,
                    cancellationToken));

        Assert.Equal(
            "Project was not found.",
            exception.Message);

        await unitOfWork
            .DidNotReceive()
            .SaveChangesAsync(
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsyncWhenTargetMemberDoesNotExistThrowsNotFoundException()
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

        var joinedAtUtc =
            CreateUtcTime();

        var ownerId =
            Guid.NewGuid();

        var targetUserId =
            Guid.NewGuid();

        var project =
            Project.Create(
                ownerId,
                "Task Manager",
                null,
                joinedAtUtc);

        currentUser.UserId.Returns(ownerId);

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
                targetUserId,
                cancellationToken)
            .Returns((ProjectMember?)null);

        var handler = CreateHandler(
            projectRepository,
            projectMemberRepository,
            unitOfWork,
            currentUser,
            clock);

        var command =
            new RemoveProjectMemberCommand(
                ProjectId: project.Id,
                UserId: targetUserId);

        var exception =
            await Assert.ThrowsAsync<ApplicationNotFoundException>(
                () => handler.HandleAsync(
                    command,
                    cancellationToken));

        Assert.Equal(
            "Project member was not found.",
            exception.Message);

        await unitOfWork
            .DidNotReceive()
            .SaveChangesAsync(
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsyncWhenTargetMemberIsAlreadyRemovedThrowsNotFoundException()
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

        var joinedAtUtc =
            CreateUtcTime();

        var ownerId =
            Guid.NewGuid();

        var targetUserId =
            Guid.NewGuid();

        var project =
            Project.Create(
                ownerId,
                "Task Manager",
                null,
                joinedAtUtc);

        var targetMembership =
            ProjectMember.Create(
                project.Id,
                targetUserId,
                ProjectMemberRole.Member,
                joinedAtUtc);

        targetMembership.Remove(
            joinedAtUtc.AddMinutes(1));

        currentUser.UserId.Returns(ownerId);

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
                targetUserId,
                cancellationToken)
            .Returns(targetMembership);

        var handler = CreateHandler(
            projectRepository,
            projectMemberRepository,
            unitOfWork,
            currentUser,
            clock);

        var command =
            new RemoveProjectMemberCommand(
                ProjectId: project.Id,
                UserId: targetUserId);

        var exception =
            await Assert.ThrowsAsync<ApplicationNotFoundException>(
                () => handler.HandleAsync(
                    command,
                    cancellationToken));

        Assert.Equal(
            "Project member was not found.",
            exception.Message);

        await unitOfWork
            .DidNotReceive()
            .SaveChangesAsync(
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsyncWhenTargetIsProjectOwnerThrowsConflictException()
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

        var joinedAtUtc =
            CreateUtcTime();

        var ownerId =
            Guid.NewGuid();

        var project =
            Project.Create(
                ownerId,
                "Task Manager",
                null,
                joinedAtUtc);

        currentUser.UserId.Returns(ownerId);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        projectRepository
            .GetByIdAsync(
                project.Id,
                cancellationToken)
            .Returns(project);

        var handler = CreateHandler(
            projectRepository,
            projectMemberRepository,
            unitOfWork,
            currentUser,
            clock);

        var command =
            new RemoveProjectMemberCommand(
                ProjectId: project.Id,
                UserId: ownerId);

        var exception =
            await Assert.ThrowsAsync<ApplicationConflictException>(
                () => handler.HandleAsync(
                    command,
                    cancellationToken));

        Assert.Equal(
            "Project owner cannot be removed.",
            exception.Message);

        await projectMemberRepository
            .DidNotReceive()
            .GetByProjectAndUserAsync(
                project.Id,
                ownerId,
                Arg.Any<CancellationToken>());

        await unitOfWork
            .DidNotReceive()
            .SaveChangesAsync(
                Arg.Any<CancellationToken>());
    }

    private static RemoveProjectMemberHandler CreateHandler(
        IProjectRepository projectRepository,
        IProjectMemberRepository projectMemberRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IClock clock)
    {
        var memberManagementPolicy =
            new ProjectMemberManagementPolicy(
                projectMemberRepository,
                currentUser);

        return new RemoveProjectMemberHandler(
            projectRepository,
            projectMemberRepository,
            unitOfWork,
            memberManagementPolicy,
            clock);
    }

    private static DateTimeOffset CreateUtcTime()
    {
        return new DateTimeOffset(
            2026,
            9,
            1,
            21,
            0,
            0,
            TimeSpan.Zero);
    }
}