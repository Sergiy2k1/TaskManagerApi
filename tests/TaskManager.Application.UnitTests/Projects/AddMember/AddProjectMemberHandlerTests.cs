using NSubstitute;
using TaskManager.Application.Abstractions.Authentication;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Abstractions.Time;
using TaskManager.Application.Common.Authorization;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Projects.AddMember;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using Xunit;

namespace TaskManager.Application.UnitTests.Projects.AddMember;

public sealed class AddProjectMemberHandlerTests
{
    [Fact]
    public async Task HandleAsyncWhenCurrentUserIsOwnerAddsProjectMember()
    {
        var projectRepository =
            Substitute.For<IProjectRepository>();

        var projectMemberRepository =
            Substitute.For<IProjectMemberRepository>();

        var userRepository =
            Substitute.For<IUserRepository>();

        var unitOfWork =
            Substitute.For<IUnitOfWork>();

        var currentUser =
            Substitute.For<ICurrentUser>();

        var clock =
            Substitute.For<IClock>();

        var now = CreateUtcTime();

        var owner =
            CreateUser(
                "owner@example.com",
                "Owner",
                now);

        var targetUser =
            CreateUser(
                "member@example.com",
                "Member",
                now);

        var project =
            Project.Create(
                owner.Id,
                "Task Manager",
                null,
                now);

        currentUser.UserId.Returns(owner.Id);
        clock.UtcNow.Returns(now);

        projectRepository
            .GetByIdAsync(
                project.Id,
                Arg.Any<CancellationToken>())
            .Returns(project);

        userRepository
            .GetByNormalizedEmailAsync(
                targetUser.NormalizedEmail,
                Arg.Any<CancellationToken>())
            .Returns(targetUser);

        projectMemberRepository
            .GetByProjectAndUserAsync(
                project.Id,
                targetUser.Id,
                Arg.Any<CancellationToken>())
            .Returns((ProjectMember?)null);

        unitOfWork
            .SaveChangesAsync(
                Arg.Any<CancellationToken>())
            .Returns(1);

        var handler = CreateHandler(
            projectRepository,
            projectMemberRepository,
            userRepository,
            unitOfWork,
            currentUser,
            clock);

        var command =
            new AddProjectMemberCommand(
                project.Id,
                targetUser.Email,
                ProjectMemberRole.Member);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        var result =
            await handler.HandleAsync(
                command,
                cancellationToken);

        Assert.Equal(project.Id, result.ProjectId);
        Assert.Equal(targetUser.Id, result.UserId);
        Assert.Equal(ProjectMemberRole.Member, result.Role);
        Assert.Equal(now, result.JoinedAtUtc);

        projectMemberRepository
            .Received(1)
            .Add(
                Arg.Is<ProjectMember>(
                    member =>
                        member.Id == result.ProjectMemberId &&
                        member.ProjectId == project.Id &&
                        member.UserId == targetUser.Id &&
                        member.Role == ProjectMemberRole.Member &&
                        member.JoinedAtUtc == now &&
                        member.IsActive));

        await unitOfWork
            .Received(1)
            .SaveChangesAsync(
                cancellationToken);
    }

    [Fact]
    public async Task HandleAsyncWhenCurrentUserIsManagerAddsProjectMember()
    {
        var projectRepository =
            Substitute.For<IProjectRepository>();

        var projectMemberRepository =
            Substitute.For<IProjectMemberRepository>();

        var userRepository =
            Substitute.For<IUserRepository>();

        var unitOfWork =
            Substitute.For<IUnitOfWork>();

        var currentUser =
            Substitute.For<ICurrentUser>();

        var clock =
            Substitute.For<IClock>();

        var now = CreateUtcTime();

        var owner =
            CreateUser(
                "owner-manager@example.com",
                "Owner",
                now);

        var managerUser =
            CreateUser(
                "manager@example.com",
                "Manager",
                now);

        var targetUser =
            CreateUser(
                "target@example.com",
                "Target",
                now);

        var project =
            Project.Create(
                owner.Id,
                "Task Manager",
                null,
                now);

        var managerMembership =
            ProjectMember.Create(
                project.Id,
                managerUser.Id,
                ProjectMemberRole.Manager,
                now);

        currentUser.UserId.Returns(managerUser.Id);
        clock.UtcNow.Returns(now);

        projectRepository
            .GetByIdAsync(
                project.Id,
                Arg.Any<CancellationToken>())
            .Returns(project);

        projectMemberRepository
            .GetByProjectAndUserAsync(
                project.Id,
                managerUser.Id,
                Arg.Any<CancellationToken>())
            .Returns(managerMembership);

        userRepository
            .GetByNormalizedEmailAsync(
                targetUser.NormalizedEmail,
                Arg.Any<CancellationToken>())
            .Returns(targetUser);

        projectMemberRepository
            .GetByProjectAndUserAsync(
                project.Id,
                targetUser.Id,
                Arg.Any<CancellationToken>())
            .Returns((ProjectMember?)null);

        unitOfWork
            .SaveChangesAsync(
                Arg.Any<CancellationToken>())
            .Returns(1);

        var handler = CreateHandler(
            projectRepository,
            projectMemberRepository,
            userRepository,
            unitOfWork,
            currentUser,
            clock);

        var command =
            new AddProjectMemberCommand(
                project.Id,
                targetUser.Email,
                ProjectMemberRole.Member);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        var result =
            await handler.HandleAsync(
                command,
                cancellationToken);

        Assert.Equal(project.Id, result.ProjectId);
        Assert.Equal(targetUser.Id, result.UserId);

        projectMemberRepository
            .Received(1)
            .Add(
                Arg.Is<ProjectMember>(
                    member =>
                        member.ProjectId == project.Id &&
                        member.UserId == targetUser.Id &&
                        member.IsActive));

        await unitOfWork
            .Received(1)
            .SaveChangesAsync(
                cancellationToken);
    }

    [Fact]
    public async Task HandleAsyncWhenCurrentUserIsMemberThrowsForbiddenException()
    {
        var projectRepository =
            Substitute.For<IProjectRepository>();

        var projectMemberRepository =
            Substitute.For<IProjectMemberRepository>();

        var userRepository =
            Substitute.For<IUserRepository>();

        var unitOfWork =
            Substitute.For<IUnitOfWork>();

        var currentUser =
            Substitute.For<ICurrentUser>();

        var clock =
            Substitute.For<IClock>();

        var now = CreateUtcTime();

        var owner =
            CreateUser(
                "owner-member@example.com",
                "Owner",
                now);

        var memberUser =
            CreateUser(
                "regular-member@example.com",
                "Member",
                now);

        var project =
            Project.Create(
                owner.Id,
                "Task Manager",
                null,
                now);

        var membership =
            ProjectMember.Create(
                project.Id,
                memberUser.Id,
                ProjectMemberRole.Member,
                now);

        currentUser.UserId.Returns(memberUser.Id);

        projectRepository
            .GetByIdAsync(
                project.Id,
                Arg.Any<CancellationToken>())
            .Returns(project);

        projectMemberRepository
            .GetByProjectAndUserAsync(
                project.Id,
                memberUser.Id,
                Arg.Any<CancellationToken>())
            .Returns(membership);

        var handler = CreateHandler(
            projectRepository,
            projectMemberRepository,
            userRepository,
            unitOfWork,
            currentUser,
            clock);

        var command =
            new AddProjectMemberCommand(
                project.Id,
                "target@example.com",
                ProjectMemberRole.Member);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        var exception =
            await Assert.ThrowsAsync<ApplicationForbiddenException>(
                () => handler.HandleAsync(
                    command,
                    cancellationToken));

        Assert.Equal(
            "You do not have permission to manage project members.",
            exception.Message);

        await userRepository
            .DidNotReceive()
            .GetByNormalizedEmailAsync(
                Arg.Any<string>(),
                Arg.Any<CancellationToken>());

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

        var userRepository =
            Substitute.For<IUserRepository>();

        var unitOfWork =
            Substitute.For<IUnitOfWork>();

        var currentUser =
            Substitute.For<ICurrentUser>();

        var clock =
            Substitute.For<IClock>();

        var now = CreateUtcTime();

        var owner =
            CreateUser(
                "owner-outsider@example.com",
                "Owner",
                now);

        var outsider =
            CreateUser(
                "outsider@example.com",
                "Outsider",
                now);

        var project =
            Project.Create(
                owner.Id,
                "Task Manager",
                null,
                now);

        currentUser.UserId.Returns(outsider.Id);

        projectRepository
            .GetByIdAsync(
                project.Id,
                Arg.Any<CancellationToken>())
            .Returns(project);

        projectMemberRepository
            .GetByProjectAndUserAsync(
                project.Id,
                outsider.Id,
                Arg.Any<CancellationToken>())
            .Returns((ProjectMember?)null);

        var handler = CreateHandler(
            projectRepository,
            projectMemberRepository,
            userRepository,
            unitOfWork,
            currentUser,
            clock);

        var command =
            new AddProjectMemberCommand(
                project.Id,
                "target@example.com",
                ProjectMemberRole.Member);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        var exception =
            await Assert.ThrowsAsync<ApplicationNotFoundException>(
                () => handler.HandleAsync(
                    command,
                    cancellationToken));

        Assert.Equal(
            "Project was not found.",
            exception.Message);

        await userRepository
            .DidNotReceive()
            .GetByNormalizedEmailAsync(
                Arg.Any<string>(),
                Arg.Any<CancellationToken>());

        await unitOfWork
            .DidNotReceive()
            .SaveChangesAsync(
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsyncWhenTargetUserDoesNotExistThrowsNotFoundException()
    {
        var projectRepository =
            Substitute.For<IProjectRepository>();

        var projectMemberRepository =
            Substitute.For<IProjectMemberRepository>();

        var userRepository =
            Substitute.For<IUserRepository>();

        var unitOfWork =
            Substitute.For<IUnitOfWork>();

        var currentUser =
            Substitute.For<ICurrentUser>();

        var clock =
            Substitute.For<IClock>();

        var now = CreateUtcTime();

        var owner =
            CreateUser(
                "owner-target@example.com",
                "Owner",
                now);

        var project =
            Project.Create(
                owner.Id,
                "Task Manager",
                null,
                now);

        currentUser.UserId.Returns(owner.Id);

        projectRepository
            .GetByIdAsync(
                project.Id,
                Arg.Any<CancellationToken>())
            .Returns(project);

        userRepository
            .GetByNormalizedEmailAsync(
                User.NormalizeEmail("missing@example.com"),
                Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var handler = CreateHandler(
            projectRepository,
            projectMemberRepository,
            userRepository,
            unitOfWork,
            currentUser,
            clock);

        var command =
            new AddProjectMemberCommand(
                project.Id,
                "missing@example.com",
                ProjectMemberRole.Member);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        var exception =
            await Assert.ThrowsAsync<ApplicationNotFoundException>(
                () => handler.HandleAsync(
                    command,
                    cancellationToken));

        Assert.Equal(
            "User was not found.",
            exception.Message);

        projectMemberRepository
            .DidNotReceive()
            .Add(
                Arg.Any<ProjectMember>());

        await unitOfWork
            .DidNotReceive()
            .SaveChangesAsync(
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsyncWhenTargetUserIsAlreadyActiveMemberThrowsConflictException()
    {
        var projectRepository =
            Substitute.For<IProjectRepository>();

        var projectMemberRepository =
            Substitute.For<IProjectMemberRepository>();

        var userRepository =
            Substitute.For<IUserRepository>();

        var unitOfWork =
            Substitute.For<IUnitOfWork>();

        var currentUser =
            Substitute.For<ICurrentUser>();

        var clock =
            Substitute.For<IClock>();

        var now = CreateUtcTime();

        var owner =
            CreateUser(
                "owner-active@example.com",
                "Owner",
                now);

        var targetUser =
            CreateUser(
                "active@example.com",
                "Active Member",
                now);

        var project =
            Project.Create(
                owner.Id,
                "Task Manager",
                null,
                now);

        var existingMembership =
            ProjectMember.Create(
                project.Id,
                targetUser.Id,
                ProjectMemberRole.Member,
                now);

        currentUser.UserId.Returns(owner.Id);

        projectRepository
            .GetByIdAsync(
                project.Id,
                Arg.Any<CancellationToken>())
            .Returns(project);

        userRepository
            .GetByNormalizedEmailAsync(
                targetUser.NormalizedEmail,
                Arg.Any<CancellationToken>())
            .Returns(targetUser);

        projectMemberRepository
            .GetByProjectAndUserAsync(
                project.Id,
                targetUser.Id,
                Arg.Any<CancellationToken>())
            .Returns(existingMembership);

        var handler = CreateHandler(
            projectRepository,
            projectMemberRepository,
            userRepository,
            unitOfWork,
            currentUser,
            clock);

        var command =
            new AddProjectMemberCommand(
                project.Id,
                targetUser.Email,
                ProjectMemberRole.Member);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        var exception =
            await Assert.ThrowsAsync<ApplicationConflictException>(
                () => handler.HandleAsync(
                    command,
                    cancellationToken));

        Assert.Equal(
            "User is already an active project member.",
            exception.Message);

        projectMemberRepository
            .DidNotReceive()
            .Add(
                Arg.Any<ProjectMember>());

        await unitOfWork
            .DidNotReceive()
            .SaveChangesAsync(
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsyncWhenTargetUserWasRemovedRestoresMembership()
    {
        var projectRepository =
            Substitute.For<IProjectRepository>();

        var projectMemberRepository =
            Substitute.For<IProjectMemberRepository>();

        var userRepository =
            Substitute.For<IUserRepository>();

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

        var restoredAtUtc =
            removedAtUtc.AddMinutes(1);

        var owner =
            CreateUser(
                "owner-restore@example.com",
                "Owner",
                joinedAtUtc);

        var targetUser =
            CreateUser(
                "restore@example.com",
                "Restored Member",
                joinedAtUtc);

        var project =
            Project.Create(
                owner.Id,
                "Task Manager",
                null,
                joinedAtUtc);

        var existingMembership =
            ProjectMember.Create(
                project.Id,
                targetUser.Id,
                ProjectMemberRole.Member,
                joinedAtUtc);

        existingMembership.Remove(
            removedAtUtc);

        currentUser.UserId.Returns(owner.Id);
        clock.UtcNow.Returns(restoredAtUtc);

        projectRepository
            .GetByIdAsync(
                project.Id,
                Arg.Any<CancellationToken>())
            .Returns(project);

        userRepository
            .GetByNormalizedEmailAsync(
                targetUser.NormalizedEmail,
                Arg.Any<CancellationToken>())
            .Returns(targetUser);

        projectMemberRepository
            .GetByProjectAndUserAsync(
                project.Id,
                targetUser.Id,
                Arg.Any<CancellationToken>())
            .Returns(existingMembership);

        unitOfWork
            .SaveChangesAsync(
                Arg.Any<CancellationToken>())
            .Returns(1);

        var handler = CreateHandler(
            projectRepository,
            projectMemberRepository,
            userRepository,
            unitOfWork,
            currentUser,
            clock);

        var command =
            new AddProjectMemberCommand(
                project.Id,
                targetUser.Email,
                ProjectMemberRole.Manager);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        var result =
            await handler.HandleAsync(
                command,
                cancellationToken);

        Assert.Equal(existingMembership.Id, result.ProjectMemberId);
        Assert.Equal(ProjectMemberRole.Manager, result.Role);
        Assert.Equal(joinedAtUtc, result.JoinedAtUtc);

        Assert.True(existingMembership.IsActive);
        Assert.Equal(
            ProjectMemberRole.Manager,
            existingMembership.Role);

        Assert.Equal(
            restoredAtUtc,
            existingMembership.UpdatedAtUtc);

        Assert.Null(
            existingMembership.RemovedAtUtc);

        projectMemberRepository
            .DidNotReceive()
            .Add(
                Arg.Any<ProjectMember>());

        await unitOfWork
            .Received(1)
            .SaveChangesAsync(
                cancellationToken);
    }

    private static AddProjectMemberHandler CreateHandler(
        IProjectRepository projectRepository,
        IProjectMemberRepository projectMemberRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IClock clock)
    {
        var memberManagementPolicy =
            new ProjectMemberManagementPolicy(
                projectMemberRepository,
                currentUser);

        return new AddProjectMemberHandler(
            projectRepository,
            projectMemberRepository,
            userRepository,
            unitOfWork,
            memberManagementPolicy,
            clock);
    }

    private static User CreateUser(
        string email,
        string displayName,
        DateTimeOffset createdAtUtc)
    {
        return User.Create(
            email,
            displayName,
            "password-hash",
            createdAtUtc);
    }

    private static DateTimeOffset CreateUtcTime()
    {
        return new DateTimeOffset(
            2026,
            9,
            1,
            12,
            0,
            0,
            TimeSpan.Zero);
    }
}