using NSubstitute;
using TaskManager.Application.Abstractions.Authentication;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Abstractions.Time;
using TaskManager.Application.Common.Authorization;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Projects.AddMember;
using TaskManager.Application.Projects.ChangeMemberRole;
using TaskManager.Application.Projects.RemoveMember;
using TaskManager.Application.Tasks.Assign;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using Xunit;

namespace TaskManager.Application.UnitTests.Hardening;

public sealed class FinalHardeningTests
{
    [Fact]
    public async Task AddMemberWhenTargetIsProjectOwnerThrowsConflict()
    {
        var projectRepository = Substitute.For<IProjectRepository>();
        var memberRepository = Substitute.For<IProjectMemberRepository>();
        var userRepository = Substitute.For<IUserRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var currentUser = Substitute.For<ICurrentUser>();
        var clock = Substitute.For<IClock>();

        var now = CreateUtcTime();
        var owner = User.Create(
            "owner@example.com",
            "Project Owner",
            "hash",
            now);
        var project = Project.Create(
            owner.Id,
            "Owner Membership Guard",
            null,
            now);
        var token = TestContext.Current.CancellationToken;

        currentUser.UserId.Returns(owner.Id);
        projectRepository.GetByIdAsync(project.Id, token).Returns(project);
        userRepository
            .GetByNormalizedEmailAsync(
                User.NormalizeEmail(owner.Email),
                token)
            .Returns(owner);

        var handler = new AddProjectMemberHandler(
            projectRepository,
            memberRepository,
            userRepository,
            unitOfWork,
            new ProjectMemberManagementPolicy(memberRepository, currentUser),
            clock);

        var exception = await Assert.ThrowsAsync<ApplicationConflictException>(
            () => handler.HandleAsync(
                new AddProjectMemberCommand(
                    project.Id,
                    owner.Email,
                    ProjectMemberRole.Member),
                token));

        Assert.Equal(
            "Project owner cannot be added as a member.",
            exception.Message);

        memberRepository.DidNotReceive().Add(Arg.Any<ProjectMember>());
        await unitOfWork
            .DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddMemberWhenProjectIsArchivedThrowsConflict()
    {
        var projectRepository = Substitute.For<IProjectRepository>();
        var memberRepository = Substitute.For<IProjectMemberRepository>();
        var userRepository = Substitute.For<IUserRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var currentUser = Substitute.For<ICurrentUser>();
        var clock = Substitute.For<IClock>();

        var now = CreateUtcTime();
        var ownerId = Guid.NewGuid();
        var project = Project.Create(ownerId, "Archived Members", null, now);
        project.Archive(now.AddMinutes(1));
        var token = TestContext.Current.CancellationToken;

        currentUser.UserId.Returns(ownerId);
        projectRepository.GetByIdAsync(project.Id, token).Returns(project);

        var handler = new AddProjectMemberHandler(
            projectRepository,
            memberRepository,
            userRepository,
            unitOfWork,
            new ProjectMemberManagementPolicy(memberRepository, currentUser),
            clock);

        var exception = await Assert.ThrowsAsync<ApplicationConflictException>(
            () => handler.HandleAsync(
                new AddProjectMemberCommand(
                    project.Id,
                    "member@example.com",
                    ProjectMemberRole.Member),
                token));

        Assert.Equal(
            "Cannot add members to an archived project.",
            exception.Message);

        await userRepository
            .DidNotReceive()
            .GetByNormalizedEmailAsync(
                Arg.Any<string>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangeMemberRoleWhenProjectIsArchivedThrowsConflict()
    {
        var projectRepository = Substitute.For<IProjectRepository>();
        var memberRepository = Substitute.For<IProjectMemberRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var currentUser = Substitute.For<ICurrentUser>();
        var clock = Substitute.For<IClock>();

        var now = CreateUtcTime();
        var ownerId = Guid.NewGuid();
        var project = Project.Create(ownerId, "Archived Roles", null, now);
        project.Archive(now.AddMinutes(1));
        var token = TestContext.Current.CancellationToken;

        currentUser.UserId.Returns(ownerId);
        projectRepository.GetByIdAsync(project.Id, token).Returns(project);

        var handler = new ChangeProjectMemberRoleHandler(
            projectRepository,
            memberRepository,
            unitOfWork,
            new ProjectMemberManagementPolicy(memberRepository, currentUser),
            clock);

        var exception = await Assert.ThrowsAsync<ApplicationConflictException>(
            () => handler.HandleAsync(
                new ChangeProjectMemberRoleCommand(
                    project.Id,
                    Guid.NewGuid(),
                    ProjectMemberRole.Manager),
                token));

        Assert.Equal(
            "Cannot change member roles in an archived project.",
            exception.Message);
    }

    [Fact]
    public async Task RemoveMemberWhenProjectIsArchivedThrowsConflict()
    {
        var projectRepository = Substitute.For<IProjectRepository>();
        var memberRepository = Substitute.For<IProjectMemberRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var currentUser = Substitute.For<ICurrentUser>();
        var clock = Substitute.For<IClock>();

        var now = CreateUtcTime();
        var ownerId = Guid.NewGuid();
        var project = Project.Create(ownerId, "Archived Removal", null, now);
        project.Archive(now.AddMinutes(1));
        var token = TestContext.Current.CancellationToken;

        currentUser.UserId.Returns(ownerId);
        projectRepository.GetByIdAsync(project.Id, token).Returns(project);

        var handler = new RemoveProjectMemberHandler(
            projectRepository,
            memberRepository,
            unitOfWork,
            new ProjectMemberManagementPolicy(memberRepository, currentUser),
            clock);

        var exception = await Assert.ThrowsAsync<ApplicationConflictException>(
            () => handler.HandleAsync(
                new RemoveProjectMemberCommand(
                    project.Id,
                    Guid.NewGuid()),
                token));

        Assert.Equal(
            "Cannot remove members from an archived project.",
            exception.Message);
    }

    [Fact]
    public async Task AssignTaskAllowsProjectOwnerWithoutMembershipRow()
    {
        var projectRepository = Substitute.For<IProjectRepository>();
        var memberRepository = Substitute.For<IProjectMemberRepository>();
        var taskRepository = Substitute.For<ITaskItemRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var currentUser = Substitute.For<ICurrentUser>();
        var clock = Substitute.For<IClock>();

        var now = CreateUtcTime();
        var ownerId = Guid.NewGuid();
        var changedAtUtc = now.AddMinutes(1);
        var project = Project.Create(ownerId, "Owner Assignment", null, now);
        var task = TaskItem.Create(
            project.Id,
            ownerId,
            "Assign owner",
            null,
            TaskPriority.Medium,
            null,
            now);
        var token = TestContext.Current.CancellationToken;

        currentUser.UserId.Returns(ownerId);
        clock.UtcNow.Returns(changedAtUtc);
        projectRepository.GetByIdAsync(project.Id, token).Returns(project);
        taskRepository.GetByIdAsync(task.Id, token).Returns(task);
        unitOfWork.SaveChangesAsync(token).Returns(1);

        var handler = new AssignTaskHandler(
            projectRepository,
            memberRepository,
            taskRepository,
            unitOfWork,
            new ProjectAccessPolicy(memberRepository, currentUser),
            clock);

        var result = await handler.HandleAsync(
            new AssignTaskCommand(
                project.Id,
                task.Id,
                ownerId),
            token);

        Assert.Equal(ownerId, result.AssigneeId);
        Assert.Equal(ownerId, task.AssigneeId);
        Assert.Equal(changedAtUtc, result.UpdatedAtUtc);

        await memberRepository
            .DidNotReceive()
            .GetByProjectAndUserAsync(
                project.Id,
                ownerId,
                Arg.Any<CancellationToken>());

        await unitOfWork
            .Received(1)
            .SaveChangesAsync(token);
    }

    private static DateTimeOffset CreateUtcTime() =>
        new(2026, 9, 12, 18, 0, 0, TimeSpan.Zero);
}
