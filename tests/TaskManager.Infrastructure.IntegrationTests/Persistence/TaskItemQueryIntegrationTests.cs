using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using TaskManager.Infrastructure.IntegrationTests.Database;
using TaskManager.Infrastructure.Persistence.Repositories;
using Xunit;

namespace TaskManager.Infrastructure.IntegrationTests.Persistence;

[Collection(PostgreSqlCollectionDefinition.Name)]
public sealed class TaskItemQueryIntegrationTests
{
    private readonly PostgreSqlFixture _fixture;

    public TaskItemQueryIntegrationTests(
        PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetPageByProjectAppliesFilters()
    {
        var cancellationToken =
            TestContext.Current.CancellationToken;

        var now =
            DateTimeOffset.UtcNow;

        var owner =
            User.Create(
                $"query-owner-{Guid.NewGuid():N}@example.com",
                "Query Owner",
                "integration-password-hash",
                now);

        var assignee =
            User.Create(
                $"query-assignee-{Guid.NewGuid():N}@example.com",
                "Query Assignee",
                "integration-password-hash",
                now);

        var project =
            Project.Create(
                owner.Id,
                "Task Query Project",
                null,
                now);

        var matchingTask =
            TaskItem.Create(
                project.Id,
                owner.Id,
                "Release Zebra",
                "Production release checklist",
                TaskPriority.High,
                now.AddDays(3),
                now.AddMinutes(1));

        matchingTask.Assign(
            assignee.Id,
            now.AddMinutes(2));

        matchingTask.ChangeStatus(
            TaskItemStatus.Todo,
            now.AddMinutes(3));

        matchingTask.ChangeStatus(
            TaskItemStatus.InProgress,
            now.AddMinutes(4));

        var wrongPriority =
            TaskItem.Create(
                project.Id,
                owner.Id,
                "Release Low",
                null,
                TaskPriority.Low,
                now.AddDays(3),
                now.AddMinutes(5));

        var wrongSearch =
            TaskItem.Create(
                project.Id,
                owner.Id,
                "Database cleanup",
                null,
                TaskPriority.High,
                now.AddDays(3),
                now.AddMinutes(6));

        await using var dbContext =
            _fixture.CreateDbContext();

        dbContext.Users.AddRange(
            owner,
            assignee);

        dbContext.Projects.Add(
            project);

        dbContext.TaskItems.AddRange(
            matchingTask,
            wrongPriority,
            wrongSearch);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        dbContext.ChangeTracker.Clear();

        var repository =
            new TaskItemRepository(
                dbContext);

        var result =
            await repository.GetPageByProjectAsync(
                new TaskItemQueryOptions(
                    ProjectId: project.Id,
                    Page: 1,
                    PageSize: 20,
                    Status: TaskItemStatus.InProgress,
                    Priority: TaskPriority.High,
                    AssigneeId: assignee.Id,
                    DueFromUtc: now.AddDays(2),
                    DueToUtc: now.AddDays(4),
                    Search: "release zebra",
                    SortBy: TaskItemSortBy.CreatedAt,
                    SortDirection: SortDirection.Asc),
                cancellationToken);

        Assert.Single(result);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1, result.TotalPages);

        Assert.Equal(
            matchingTask.Id,
            result[0].Id);

        Assert.Empty(
            dbContext.ChangeTracker
                .Entries<TaskItem>());
    }

    [Fact]
    public async Task GetPageByProjectSortsAndPaginates()
    {
        var cancellationToken =
            TestContext.Current.CancellationToken;

        var now =
            DateTimeOffset.UtcNow;

        var owner =
            User.Create(
                $"page-owner-{Guid.NewGuid():N}@example.com",
                "Page Owner",
                "integration-password-hash",
                now);

        var project =
            Project.Create(
                owner.Id,
                "Paging Project",
                null,
                now);

        var alpha =
            TaskItem.Create(
                project.Id,
                owner.Id,
                "Alpha",
                null,
                TaskPriority.Medium,
                null,
                now.AddMinutes(1));

        var zebra =
            TaskItem.Create(
                project.Id,
                owner.Id,
                "Zebra",
                null,
                TaskPriority.High,
                null,
                now.AddMinutes(2));

        var middle =
            TaskItem.Create(
                project.Id,
                owner.Id,
                "Middle",
                null,
                TaskPriority.Low,
                null,
                now.AddMinutes(3));

        await using var dbContext =
            _fixture.CreateDbContext();

        dbContext.Users.Add(owner);
        dbContext.Projects.Add(project);
        dbContext.TaskItems.AddRange(
            alpha,
            zebra,
            middle);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        dbContext.ChangeTracker.Clear();

        var repository =
            new TaskItemRepository(
                dbContext);

        var firstPage =
            await repository.GetPageByProjectAsync(
                new TaskItemQueryOptions(
                    project.Id,
                    Page: 1,
                    PageSize: 2,
                    Status: null,
                    Priority: null,
                    AssigneeId: null,
                    DueFromUtc: null,
                    DueToUtc: null,
                    Search: null,
                    SortBy: TaskItemSortBy.Title,
                    SortDirection: SortDirection.Desc),
                cancellationToken);

        var secondPage =
            await repository.GetPageByProjectAsync(
                new TaskItemQueryOptions(
                    project.Id,
                    Page: 2,
                    PageSize: 2,
                    Status: null,
                    Priority: null,
                    AssigneeId: null,
                    DueFromUtc: null,
                    DueToUtc: null,
                    Search: null,
                    SortBy: TaskItemSortBy.Title,
                    SortDirection: SortDirection.Desc),
                cancellationToken);

        Assert.Equal(3, firstPage.TotalCount);
        Assert.Equal(2, firstPage.TotalPages);
        Assert.Equal(2, firstPage.Count);

        Assert.Equal(
            "Zebra",
            firstPage[0].Title);

        Assert.Equal(
            "Middle",
            firstPage[1].Title);

        Assert.Single(secondPage);

        Assert.Equal(
            "Alpha",
            secondPage[0].Title);
    }
}
