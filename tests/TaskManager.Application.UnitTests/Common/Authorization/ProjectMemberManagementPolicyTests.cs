using NSubstitute;
using TaskManager.Application.Abstractions.Authentication;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Common.Authorization;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using Xunit;

namespace TaskManager.Application.UnitTests.Common.Authorization;

public sealed class ProjectMemberManagementPolicyTests
{
    [Fact]
    public async Task EnsureCanManageMembersAsyncWhenCurrentUserIsOwnerAllowsAccess()
    {
        var projectMemberRepository =
            Substitute.For<IProjectMemberRepository>();

        var currentUser =
            Substitute.For<ICurrentUser>();

        var ownerId =
            Guid.NewGuid();

        var projectId =
            Guid.NewGuid();

        currentUser.UserId.Returns(
            ownerId);

        var policy =
            new ProjectMemberManagementPolicy(
                projectMemberRepository,
                currentUser);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        await policy.EnsureCanManageMembersAsync(
            ownerId,
            projectId,
            cancellationToken);

        await projectMemberRepository
            .DidNotReceive()
            .GetByProjectAndUserAsync(
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnsureCanManageMembersAsyncWhenCurrentUserIsManagerAllowsAccess()
    {
        var projectMemberRepository =
            Substitute.For<IProjectMemberRepository>();

        var currentUser =
            Substitute.For<ICurrentUser>();

        var now =
            CreateUtcTime();

        var ownerId =
            Guid.NewGuid();

        var managerId =
            Guid.NewGuid();

        var projectId =
            Guid.NewGuid();

        var membership =
            ProjectMember.Create(
                projectId,
                managerId,
                ProjectMemberRole.Manager,
                now);

        currentUser.UserId.Returns(
            managerId);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        projectMemberRepository
            .GetByProjectAndUserAsync(
                projectId,
                managerId,
                cancellationToken)
            .Returns(membership);

        var policy =
            new ProjectMemberManagementPolicy(
                projectMemberRepository,
                currentUser);

        await policy.EnsureCanManageMembersAsync(
            ownerId,
            projectId,
            cancellationToken);

        await projectMemberRepository
            .Received(1)
            .GetByProjectAndUserAsync(
                projectId,
                managerId,
                cancellationToken);
    }

    [Fact]
    public async Task EnsureCanManageMembersAsyncWhenCurrentUserIsMemberThrowsForbiddenException()
    {
        var projectMemberRepository =
            Substitute.For<IProjectMemberRepository>();

        var currentUser =
            Substitute.For<ICurrentUser>();

        var now =
            CreateUtcTime();

        var ownerId =
            Guid.NewGuid();

        var memberId =
            Guid.NewGuid();

        var projectId =
            Guid.NewGuid();

        var membership =
            ProjectMember.Create(
                projectId,
                memberId,
                ProjectMemberRole.Member,
                now);

        currentUser.UserId.Returns(
            memberId);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        projectMemberRepository
            .GetByProjectAndUserAsync(
                projectId,
                memberId,
                cancellationToken)
            .Returns(membership);

        var policy =
            new ProjectMemberManagementPolicy(
                projectMemberRepository,
                currentUser);

        var exception =
            await Assert.ThrowsAsync<ApplicationForbiddenException>(
                () => policy.EnsureCanManageMembersAsync(
                    ownerId,
                    projectId,
                    cancellationToken));

        Assert.Equal(
            "You do not have permission to manage project members.",
            exception.Message);
    }

    [Fact]
    public async Task EnsureCanManageMembersAsyncWhenCurrentUserIsNotMemberThrowsNotFoundException()
    {
        var projectMemberRepository =
            Substitute.For<IProjectMemberRepository>();

        var currentUser =
            Substitute.For<ICurrentUser>();

        var ownerId =
            Guid.NewGuid();

        var outsiderId =
            Guid.NewGuid();

        var projectId =
            Guid.NewGuid();

        currentUser.UserId.Returns(
            outsiderId);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        projectMemberRepository
            .GetByProjectAndUserAsync(
                projectId,
                outsiderId,
                cancellationToken)
            .Returns((ProjectMember?)null);

        var policy =
            new ProjectMemberManagementPolicy(
                projectMemberRepository,
                currentUser);

        var exception =
            await Assert.ThrowsAsync<ApplicationNotFoundException>(
                () => policy.EnsureCanManageMembersAsync(
                    ownerId,
                    projectId,
                    cancellationToken));

        Assert.Equal(
            "Project was not found.",
            exception.Message);
    }

    [Fact]
    public async Task EnsureCanManageMembersAsyncWhenCurrentUserWasRemovedThrowsNotFoundException()
    {
        var projectMemberRepository =
            Substitute.For<IProjectMemberRepository>();

        var currentUser =
            Substitute.For<ICurrentUser>();

        var now =
            CreateUtcTime();

        var ownerId =
            Guid.NewGuid();

        var managerId =
            Guid.NewGuid();

        var projectId =
            Guid.NewGuid();

        var membership =
            ProjectMember.Create(
                projectId,
                managerId,
                ProjectMemberRole.Manager,
                now);

        membership.Remove(
            now.AddMinutes(1));

        currentUser.UserId.Returns(
            managerId);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        projectMemberRepository
            .GetByProjectAndUserAsync(
                projectId,
                managerId,
                cancellationToken)
            .Returns(membership);

        var policy =
            new ProjectMemberManagementPolicy(
                projectMemberRepository,
                currentUser);

        var exception =
            await Assert.ThrowsAsync<ApplicationNotFoundException>(
                () => policy.EnsureCanManageMembersAsync(
                    ownerId,
                    projectId,
                    cancellationToken));

        Assert.Equal(
            "Project was not found.",
            exception.Message);
    }

    private static DateTimeOffset CreateUtcTime()
    {
        return new DateTimeOffset(
            2026,
            9,
            3,
            12,
            0,
            0,
            TimeSpan.Zero);
    }
}