using NSubstitute;
using TaskManager.Application.Abstractions.Authentication;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Common.Authorization;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Common.Pagination;
using TaskManager.Application.Tasks.GetByProject;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using Xunit;

namespace TaskManager.Application.UnitTests.Tasks.GetByProject;

public sealed class GetProjectTasksHandlerTests
{
    [Fact]
    public async Task HandleAsyncWhenCurrentUserIsOwnerReturnsPagedTasks()
    {
        var projectRepository =
            Substitute.For<IProjectRepository>();

        var projectMemberRepository =
            Substitute.For<IProjectMemberRepository>();

        var taskItemRepository =
            Substitute.For<ITaskItemRepository>();

        var currentUser =
            Substitute.For<ICurrentUser>();

        var now =
            CreateUtcTime();

        var ownerId =
            Guid.NewGuid();

        var project =
            Project.Create(
                ownerId,
                "Task Project",
                null,
                now);

        var taskItem =
            TaskItem.Create(
                project.Id,
                ownerId,
                "Release task",
                "Release description",
                TaskPriority.High,
                now.AddDays(2),
                now);

        currentUser.UserId.Returns(
            ownerId);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        projectRepository
            .GetByIdAsync(
                project.Id,
                cancellationToken)
            .Returns(project);

        taskItemRepository
            .GetPageByProjectAsync(
                Arg.Any<TaskItemQueryOptions>(),
                cancellationToken)
            .Returns(
                new PagedResult<TaskItem>(
                    [taskItem],
                    page: 2,
                    pageSize: 5,
                    totalCount: 7));

        var handler =
            CreateHandler(
                projectRepository,
                projectMemberRepository,
                taskItemRepository,
                currentUser);

        var dueFrom =
            new DateTimeOffset(
                2026,
                9,
                4,
                14,
                0,
                0,
                TimeSpan.FromHours(2));

        var query =
            new GetProjectTasksQuery(
                ProjectId: project.Id,
                Page: 2,
                PageSize: 5,
                Status: TaskItemStatus.Backlog,
                Priority: TaskPriority.High,
                AssigneeId: ownerId,
                DueFromUtc: dueFrom,
                DueToUtc: dueFrom.AddDays(3),
                Search: "  release  ",
                SortBy: TaskItemSortBy.Priority,
                SortDirection: SortDirection.Desc);

        var result =
            await handler.HandleAsync(
                query,
                cancellationToken);

        Assert.Single(result);

        Assert.Equal(
            2,
            result.Page);

        Assert.Equal(
            5,
            result.PageSize);

        Assert.Equal(
            7,
            result.TotalCount);

        Assert.Equal(
            2,
            result.TotalPages);

        Assert.Equal(
            taskItem.Id,
            result[0].TaskItemId);

        await projectMemberRepository
            .DidNotReceive()
            .GetByProjectAndUserAsync(
                project.Id,
                ownerId,
                Arg.Any<CancellationToken>());

        await taskItemRepository
            .Received(1)
            .GetPageByProjectAsync(
                Arg.Is<TaskItemQueryOptions>(
                    options =>
                        options.ProjectId == project.Id &&
                        options.Page == 2 &&
                        options.PageSize == 5 &&
                        options.Status == TaskItemStatus.Backlog &&
                        options.Priority == TaskPriority.High &&
                        options.AssigneeId == ownerId &&
                        options.DueFromUtc == dueFrom.ToUniversalTime() &&
                        options.DueToUtc == dueFrom.AddDays(3).ToUniversalTime() &&
                        options.Search == "release" &&
                        options.SortBy == TaskItemSortBy.Priority &&
                        options.SortDirection == SortDirection.Desc),
                cancellationToken);
    }

    [Fact]
    public async Task HandleAsyncWhenCurrentUserIsActiveMemberReturnsTasks()
    {
        var projectRepository =
            Substitute.For<IProjectRepository>();

        var projectMemberRepository =
            Substitute.For<IProjectMemberRepository>();

        var taskItemRepository =
            Substitute.For<ITaskItemRepository>();

        var currentUser =
            Substitute.For<ICurrentUser>();

        var now =
            CreateUtcTime();

        var ownerId =
            Guid.NewGuid();

        var memberId =
            Guid.NewGuid();

        var project =
            Project.Create(
                ownerId,
                "Member Project",
                null,
                now);

        var membership =
            ProjectMember.Create(
                project.Id,
                memberId,
                ProjectMemberRole.Member,
                now);

        var taskItem =
            TaskItem.Create(
                project.Id,
                ownerId,
                "Visible task",
                null,
                TaskPriority.Medium,
                null,
                now);

        currentUser.UserId.Returns(
            memberId);

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
                memberId,
                cancellationToken)
            .Returns(membership);

        taskItemRepository
            .GetPageByProjectAsync(
                Arg.Any<TaskItemQueryOptions>(),
                cancellationToken)
            .Returns(
                new PagedResult<TaskItem>(
                    [taskItem],
                    1,
                    20,
                    1));

        var handler =
            CreateHandler(
                projectRepository,
                projectMemberRepository,
                taskItemRepository,
                currentUser);

        var result =
            await handler.HandleAsync(
                new GetProjectTasksQuery(
                    project.Id),
                cancellationToken);

        Assert.Single(result);

        Assert.Equal(
            taskItem.Id,
            result[0].TaskItemId);
    }

    [Fact]
    public async Task HandleAsyncWhenProjectHasNoTasksReturnsEmptyPage()
    {
        var projectRepository =
            Substitute.For<IProjectRepository>();

        var projectMemberRepository =
            Substitute.For<IProjectMemberRepository>();

        var taskItemRepository =
            Substitute.For<ITaskItemRepository>();

        var currentUser =
            Substitute.For<ICurrentUser>();

        var now =
            CreateUtcTime();

        var ownerId =
            Guid.NewGuid();

        var project =
            Project.Create(
                ownerId,
                "Empty Project",
                null,
                now);

        currentUser.UserId.Returns(
            ownerId);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        projectRepository
            .GetByIdAsync(
                project.Id,
                cancellationToken)
            .Returns(project);

        taskItemRepository
            .GetPageByProjectAsync(
                Arg.Any<TaskItemQueryOptions>(),
                cancellationToken)
            .Returns(
                new PagedResult<TaskItem>(
                    Array.Empty<TaskItem>(),
                    1,
                    20,
                    0));

        var handler =
            CreateHandler(
                projectRepository,
                projectMemberRepository,
                taskItemRepository,
                currentUser);

        var result =
            await handler.HandleAsync(
                new GetProjectTasksQuery(
                    project.Id),
                cancellationToken);

        Assert.Empty(result);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public async Task HandleAsyncWhenProjectIsArchivedStillReturnsTasks()
    {
        var projectRepository =
            Substitute.For<IProjectRepository>();

        var projectMemberRepository =
            Substitute.For<IProjectMemberRepository>();

        var taskItemRepository =
            Substitute.For<ITaskItemRepository>();

        var currentUser =
            Substitute.For<ICurrentUser>();

        var now =
            CreateUtcTime();

        var ownerId =
            Guid.NewGuid();

        var project =
            Project.Create(
                ownerId,
                "Archived Project",
                null,
                now);

        var taskItem =
            TaskItem.Create(
                project.Id,
                ownerId,
                "Archived task",
                null,
                TaskPriority.Medium,
                null,
                now);

        project.Archive(
            now.AddMinutes(1));

        currentUser.UserId.Returns(
            ownerId);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        projectRepository
            .GetByIdAsync(
                project.Id,
                cancellationToken)
            .Returns(project);

        taskItemRepository
            .GetPageByProjectAsync(
                Arg.Any<TaskItemQueryOptions>(),
                cancellationToken)
            .Returns(
                new PagedResult<TaskItem>(
                    [taskItem],
                    1,
                    20,
                    1));

        var handler =
            CreateHandler(
                projectRepository,
                projectMemberRepository,
                taskItemRepository,
                currentUser);

        var result =
            await handler.HandleAsync(
                new GetProjectTasksQuery(
                    project.Id),
                cancellationToken);

        Assert.Single(result);
    }

    [Fact]
    public async Task HandleAsyncWhenCurrentUserIsOutsiderThrowsNotFoundException()
    {
        var projectRepository =
            Substitute.For<IProjectRepository>();

        var projectMemberRepository =
            Substitute.For<IProjectMemberRepository>();

        var taskItemRepository =
            Substitute.For<ITaskItemRepository>();

        var currentUser =
            Substitute.For<ICurrentUser>();

        var ownerId =
            Guid.NewGuid();

        var outsiderId =
            Guid.NewGuid();

        var project =
            Project.Create(
                ownerId,
                "Private Project",
                null,
                CreateUtcTime());

        currentUser.UserId.Returns(
            outsiderId);

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
                outsiderId,
                cancellationToken)
            .Returns((ProjectMember?)null);

        var handler =
            CreateHandler(
                projectRepository,
                projectMemberRepository,
                taskItemRepository,
                currentUser);

        var exception =
            await Assert.ThrowsAsync<ApplicationNotFoundException>(
                () => handler.HandleAsync(
                    new GetProjectTasksQuery(
                        project.Id),
                    cancellationToken));

        Assert.Equal(
            "Project was not found.",
            exception.Message);

        await taskItemRepository
            .DidNotReceive()
            .GetPageByProjectAsync(
                Arg.Any<TaskItemQueryOptions>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsyncWhenProjectDoesNotExistThrowsNotFoundException()
    {
        var projectRepository =
            Substitute.For<IProjectRepository>();

        var projectMemberRepository =
            Substitute.For<IProjectMemberRepository>();

        var taskItemRepository =
            Substitute.For<ITaskItemRepository>();

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
                taskItemRepository,
                currentUser);

        await Assert.ThrowsAsync<ApplicationNotFoundException>(
            () => handler.HandleAsync(
                new GetProjectTasksQuery(
                    projectId),
                cancellationToken));

        await taskItemRepository
            .DidNotReceive()
            .GetPageByProjectAsync(
                Arg.Any<TaskItemQueryOptions>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsyncWhenProjectIdIsEmptyThrowsValidationException()
    {
        var handler =
            CreateHandler(
                Substitute.For<IProjectRepository>(),
                Substitute.For<IProjectMemberRepository>(),
                Substitute.For<ITaskItemRepository>(),
                Substitute.For<ICurrentUser>());

        var exception =
            await Assert.ThrowsAsync<ApplicationValidationException>(
                () => handler.HandleAsync(
                    new GetProjectTasksQuery(
                        Guid.Empty),
                    TestContext.Current.CancellationToken));

        Assert.Equal(
            "Project identifier cannot be empty.",
            exception.Message);
    }

    [Theory]
    [InlineData(0, 20, "Page must be greater than or equal to 1.")]
    [InlineData(1, 0, "Page size must be between 1 and 100.")]
    [InlineData(1, 101, "Page size must be between 1 and 100.")]
    public async Task HandleAsyncWhenPaginationIsInvalidThrowsValidationException(
        int page,
        int pageSize,
        string expectedMessage)
    {
        var handler =
            CreateHandler(
                Substitute.For<IProjectRepository>(),
                Substitute.For<IProjectMemberRepository>(),
                Substitute.For<ITaskItemRepository>(),
                Substitute.For<ICurrentUser>());

        var exception =
            await Assert.ThrowsAsync<ApplicationValidationException>(
                () => handler.HandleAsync(
                    new GetProjectTasksQuery(
                        Guid.NewGuid(),
                        Page: page,
                        PageSize: pageSize),
                    TestContext.Current.CancellationToken));

        Assert.Equal(
            expectedMessage,
            exception.Message);
    }

    [Fact]
    public async Task HandleAsyncWhenDueRangeIsInvalidThrowsValidationException()
    {
        var handler =
            CreateHandler(
                Substitute.For<IProjectRepository>(),
                Substitute.For<IProjectMemberRepository>(),
                Substitute.For<ITaskItemRepository>(),
                Substitute.For<ICurrentUser>());

        var from =
            CreateUtcTime().AddDays(2);

        var exception =
            await Assert.ThrowsAsync<ApplicationValidationException>(
                () => handler.HandleAsync(
                    new GetProjectTasksQuery(
                        Guid.NewGuid(),
                        DueFromUtc: from,
                        DueToUtc: from.AddDays(-1)),
                    TestContext.Current.CancellationToken));

        Assert.Equal(
            "Due date range is invalid.",
            exception.Message);
    }

    private static GetProjectTasksHandler CreateHandler(
        IProjectRepository projectRepository,
        IProjectMemberRepository projectMemberRepository,
        ITaskItemRepository taskItemRepository,
        ICurrentUser currentUser)
    {
        var projectAccessPolicy =
            new ProjectAccessPolicy(
                projectMemberRepository,
                currentUser);

        return new GetProjectTasksHandler(
            projectRepository,
            taskItemRepository,
            projectAccessPolicy);
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
