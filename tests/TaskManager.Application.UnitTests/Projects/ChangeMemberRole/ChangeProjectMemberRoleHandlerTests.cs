using NSubstitute;
using TaskManager.Application.Abstractions.Authentication;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Abstractions.Time;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Projects.ChangeMemberRole;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using Xunit;

namespace TaskManager.Application.UnitTests.Projects.ChangeMemberRole;

public sealed class ChangeProjectMemberRoleHandlerTests
{
    [Fact]
    public async Task HandleAsyncWhenCurrentUserIsOwnerChangesMemberRole()
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

        var createdAtUtc =
            CreateUtcTime();

        var changedAtUtc =
            createdAtUtc.AddMinutes(1);

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

        var membership =
            ProjectMember.Create(
                project.Id,
                memberUserId,
                ProjectMemberRole.Member,
                createdAtUtc);

        currentUser.UserId.Returns(ownerId);
        clock.UtcNow.Returns(changedAtUtc);

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
            new ChangeProjectMemberRoleCommand(
                ProjectId: project.Id,
                UserId: memberUserId,
                Role: ProjectMemberRole.Manager);

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
            ProjectMemberRole.Manager,
            result.Role);

        Assert.Equal(
            changedAtUtc,
            result.UpdatedAtUtc);

        Assert.Equal(
            ProjectMemberRole.Manager,
            membership.Role);

        Assert.Equal(
            changedAtUtc,
            membership.UpdatedAtUtc);

        await unitOfWork
            .Received(1)
            .SaveChangesAsync(cancellationToken);
    }

    [Fact]
    public async Task HandleAsyncWhenCurrentUserIsManagerChangesMemberRole()
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

        var createdAtUtc =
            CreateUtcTime();

        var changedAtUtc =
            createdAtUtc.AddMinutes(1);

        var ownerId =
            Guid.NewGuid();

        var managerUserId =
            Guid.NewGuid();

        var memberUserId =
            Guid.NewGuid();

        var project =
            Project.Create(
                ownerId,
                "Task Manager",
                null,
                createdAtUtc);

        var managerMembership =
            ProjectMember.Create(
                project.Id,
                managerUserId,
                ProjectMemberRole.Manager,
                createdAtUtc);

        var targetMembership =
            ProjectMember.Create(
                project.Id,
                memberUserId,
                ProjectMemberRole.Member,
                createdAtUtc);

        currentUser.UserId.Returns(managerUserId);
        clock.UtcNow.Returns(changedAtUtc);

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
                memberUserId,
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
            new ChangeProjectMemberRoleCommand(
                ProjectId: project.Id,
                UserId: memberUserId,
                Role: ProjectMemberRole.Manager);

        var result =
            await handler.HandleAsync(
                command,
                cancellationToken);

        Assert.Equal(
            ProjectMemberRole.Manager,
            result.Role);

        Assert.Equal(
            ProjectMemberRole.Manager,
            targetMembership.Role);

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

        var createdAtUtc =
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
                createdAtUtc);

        var currentMembership =
            ProjectMember.Create(
                project.Id,
                currentUserId,
                ProjectMemberRole.Member,
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
            .Returns(currentMembership);

        var handler = CreateHandler(
            projectRepository,
            projectMemberRepository,
            unitOfWork,
            currentUser,
            clock);

        var command =
            new ChangeProjectMemberRoleCommand(
                ProjectId: project.Id,
                UserId: targetUserId,
                Role: ProjectMemberRole.Manager);

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

        var createdAtUtc =
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

        var handler = CreateHandler(
            projectRepository,
            projectMemberRepository,
            unitOfWork,
            currentUser,
            clock);

        var command =
            new ChangeProjectMemberRoleCommand(
                ProjectId: project.Id,
                UserId: targetUserId,
                Role: ProjectMemberRole.Manager);

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
    public async Task HandleAsyncWhenTargetMemberIsRemovedThrowsNotFoundException()
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

        var createdAtUtc =
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
                createdAtUtc);

        var targetMembership =
            ProjectMember.Create(
                project.Id,
                targetUserId,
                ProjectMemberRole.Member,
                createdAtUtc);

        targetMembership.Remove(
            createdAtUtc.AddMinutes(1));

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
            new ChangeProjectMemberRoleCommand(
                ProjectId: project.Id,
                UserId: targetUserId,
                Role: ProjectMemberRole.Manager);

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

        var createdAtUtc =
            CreateUtcTime();

        var ownerId =
            Guid.NewGuid();

        var project =
            Project.Create(
                ownerId,
                "Task Manager",
                null,
                createdAtUtc);

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
            new ChangeProjectMemberRoleCommand(
                ProjectId: project.Id,
                UserId: ownerId,
                Role: ProjectMemberRole.Member);

        var exception =
            await Assert.ThrowsAsync<ApplicationConflictException>(
                () => handler.HandleAsync(
                    command,
                    cancellationToken));

        Assert.Equal(
            "Project owner role cannot be changed.",
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

    [Fact]
    public async Task HandleAsyncWhenRoleIsUnchangedReturnsSuccessWithoutChangingTimestamp()
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

        var createdAtUtc =
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
                createdAtUtc);

        var targetMembership =
            ProjectMember.Create(
                project.Id,
                targetUserId,
                ProjectMemberRole.Member,
                createdAtUtc);

        currentUser.UserId.Returns(ownerId);

        clock.UtcNow.Returns(
            createdAtUtc.AddMinutes(1));

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

        unitOfWork
            .SaveChangesAsync(cancellationToken)
            .Returns(0);

        var handler = CreateHandler(
            projectRepository,
            projectMemberRepository,
            unitOfWork,
            currentUser,
            clock);

        var command =
            new ChangeProjectMemberRoleCommand(
                ProjectId: project.Id,
                UserId: targetUserId,
                Role: ProjectMemberRole.Member);

        var result =
            await handler.HandleAsync(
                command,
                cancellationToken);

        Assert.Equal(
            ProjectMemberRole.Member,
            result.Role);

        Assert.Null(
            result.UpdatedAtUtc);

        Assert.Null(
            targetMembership.UpdatedAtUtc);

        await unitOfWork
            .Received(1)
            .SaveChangesAsync(cancellationToken);
    }

    private static ChangeProjectMemberRoleHandler CreateHandler(
        IProjectRepository projectRepository,
        IProjectMemberRepository projectMemberRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IClock clock)
    {
        return new ChangeProjectMemberRoleHandler(
            projectRepository,
            projectMemberRepository,
            unitOfWork,
            currentUser,
            clock);
    }

    private static DateTimeOffset CreateUtcTime()
    {
        return new DateTimeOffset(
            2026,
            9,
            1,
            20,
            0,
            0,
            TimeSpan.Zero);
    }
}