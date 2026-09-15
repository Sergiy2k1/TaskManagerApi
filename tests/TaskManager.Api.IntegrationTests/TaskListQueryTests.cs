using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TaskManager.Api.Contracts.Auth;
using TaskManager.Api.Contracts.Projects;
using TaskManager.Api.Contracts.Tasks;
using TaskManager.Api.IntegrationTests.Infrastructure;
using TaskManager.Domain.Enums;
using Xunit;

namespace TaskManager.Api.IntegrationTests;

[Collection(ApiIntegrationCollectionDefinition.Name)]
public sealed class TaskListQueryTests
{
    private readonly ApiIntegrationFixture _fixture;

    public TaskListQueryTests(
        ApiIntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task TaskListSupportsFilteringSortingAndPagination()
    {
        var cancellationToken =
            TestContext.Current.CancellationToken;

        await _fixture.ResetDatabaseAsync(
            cancellationToken);

        using var client =
            await CreateAuthenticatedClientAsync(
                cancellationToken);

        var project =
            await CreateProjectAsync(
                client,
                cancellationToken);

        await CreateTaskAsync(
            client,
            project.ProjectId,
            "Release Alpha",
            TaskPriority.High,
            cancellationToken);

        await CreateTaskAsync(
            client,
            project.ProjectId,
            "Release Zebra",
            TaskPriority.High,
            cancellationToken);

        await CreateTaskAsync(
            client,
            project.ProjectId,
            "Backlog Notes",
            TaskPriority.Low,
            cancellationToken);

        var response =
            await client.GetAsync(
                $"/api/projects/{project.ProjectId}/tasks" +
                "?page=1&pageSize=1" +
                "&priority=High" +
                "&search=release" +
                "&sortBy=Title" +
                "&sortDirection=Desc",
                cancellationToken);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var page =
            await response.Content
                .ReadFromJsonAsync<GetProjectTasksPageResponse>(
                    cancellationToken);

        Assert.NotNull(page);

        Assert.Equal(1, page.Page);
        Assert.Equal(1, page.PageSize);
        Assert.Equal(2, page.TotalCount);
        Assert.Equal(2, page.TotalPages);
        Assert.Single(page.Items);

        Assert.Equal(
            "Release Zebra",
            page.Items[0].Title);

        Assert.Equal(
            TaskPriority.High,
            page.Items[0].Priority);
    }

    private async Task<HttpClient>
        CreateAuthenticatedClientAsync(
            CancellationToken cancellationToken)
    {
        var client =
            _fixture.CreateClient();

        var email =
            $"task-list-{Guid.NewGuid():N}@example.com";

        const string password =
            "StrongPassword123";

        var registerResponse =
            await client.PostAsJsonAsync(
                "/api/auth/register",
                new RegisterRequest(
                    email,
                    "Task List User",
                    password),
                cancellationToken);

        Assert.Equal(
            HttpStatusCode.Created,
            registerResponse.StatusCode);

        var loginResponse =
            await client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest(
                    email,
                    password),
                cancellationToken);

        var login =
            await loginResponse.Content
                .ReadFromJsonAsync<LoginResponse>(
                    cancellationToken);

        Assert.NotNull(login);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                login.AccessToken);

        return client;
    }

    private static async Task<CreateProjectResponse>
        CreateProjectAsync(
            HttpClient client,
            CancellationToken cancellationToken)
    {
        var response =
            await client.PostAsJsonAsync(
                "/api/projects",
                new CreateProjectRequest(
                    "Task List Project",
                    null),
                cancellationToken);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var project =
            await response.Content
                .ReadFromJsonAsync<CreateProjectResponse>(
                    cancellationToken);

        Assert.NotNull(project);

        return project;
    }

    private static async Task CreateTaskAsync(
        HttpClient client,
        Guid projectId,
        string title,
        TaskPriority priority,
        CancellationToken cancellationToken)
    {
        var response =
            await client.PostAsJsonAsync(
                $"/api/projects/{projectId}/tasks",
                new CreateTaskRequest(
                    title,
                    null,
                    priority,
                    null),
                cancellationToken);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);
    }
}
