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
public sealed class ProjectTaskFlowTests
{
    private readonly ApiIntegrationFixture _fixture;

    public ProjectTaskFlowTests(
        ApiIntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CreateProjectThenGetByIdReturnsPersistedProject()
    {
        var cancellationToken =
            TestContext.Current.CancellationToken;

        await _fixture.ResetDatabaseAsync(
            cancellationToken);

        using var client =
            await CreateAuthenticatedClientAsync(
                cancellationToken);

        var createdProject =
            await CreateProjectAsync(
                client,
                cancellationToken);

        var getResponse =
            await client.GetAsync(
                $"/api/projects/{createdProject.ProjectId}",
                cancellationToken);

        Assert.Equal(
            HttpStatusCode.OK,
            getResponse.StatusCode);

        var persistedProject =
            await getResponse.Content
                .ReadFromJsonAsync<GetProjectByIdResponse>(
                    cancellationToken);

        Assert.NotNull(persistedProject);

        Assert.Equal(
            createdProject.ProjectId,
            persistedProject.ProjectId);

        Assert.Equal(
            createdProject.OwnerId,
            persistedProject.OwnerId);

        Assert.Equal(
            "API Integration Project",
            persistedProject.Name);

        Assert.Equal(
            "Created through the real HTTP pipeline.",
            persistedProject.Description);

        Assert.False(
            persistedProject.IsArchived);
    }

    [Fact]
    public async Task ArchiveThenRestoreProjectPersistsLifecycleState()
    {
        var cancellationToken =
            TestContext.Current.CancellationToken;

        await _fixture.ResetDatabaseAsync(
            cancellationToken);

        using var client =
            await CreateAuthenticatedClientAsync(
                cancellationToken);

        var createdProject =
            await CreateProjectAsync(
                client,
                cancellationToken);

        var archiveResponse =
            await client.PostAsync(
                $"/api/projects/{createdProject.ProjectId}/archive",
                content: null,
                cancellationToken);

        Assert.Equal(
            HttpStatusCode.OK,
            archiveResponse.StatusCode);

        var archivedProject =
            await archiveResponse.Content
                .ReadFromJsonAsync<ProjectLifecycleResponse>(
                    cancellationToken);

        Assert.NotNull(archivedProject);
        Assert.True(
            archivedProject.IsArchived);

        Assert.NotNull(
            archivedProject.ArchivedAtUtc);

        var archivedGetResponse =
            await client.GetAsync(
                $"/api/projects/{createdProject.ProjectId}",
                cancellationToken);

        var persistedArchivedProject =
            await archivedGetResponse.Content
                .ReadFromJsonAsync<GetProjectByIdResponse>(
                    cancellationToken);

        Assert.NotNull(
            persistedArchivedProject);

        Assert.True(
            persistedArchivedProject.IsArchived);

        Assert.NotNull(
            persistedArchivedProject.ArchivedAtUtc);

        var restoreResponse =
            await client.PostAsync(
                $"/api/projects/{createdProject.ProjectId}/restore",
                content: null,
                cancellationToken);

        Assert.Equal(
            HttpStatusCode.OK,
            restoreResponse.StatusCode);

        var restoredProject =
            await restoreResponse.Content
                .ReadFromJsonAsync<ProjectLifecycleResponse>(
                    cancellationToken);

        Assert.NotNull(restoredProject);

        Assert.False(
            restoredProject.IsArchived);

        Assert.Null(
            restoredProject.ArchivedAtUtc);

        var restoredGetResponse =
            await client.GetAsync(
                $"/api/projects/{createdProject.ProjectId}",
                cancellationToken);

        var persistedRestoredProject =
            await restoredGetResponse.Content
                .ReadFromJsonAsync<GetProjectByIdResponse>(
                    cancellationToken);

        Assert.NotNull(
            persistedRestoredProject);

        Assert.False(
            persistedRestoredProject.IsArchived);

        Assert.Null(
            persistedRestoredProject.ArchivedAtUtc);
    }

    [Fact]
    public async Task CreateTaskThenGetByIdReturnsPersistedTask()
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

        var createTaskResponse =
            await client.PostAsJsonAsync(
                $"/api/projects/{project.ProjectId}/tasks",
                new CreateTaskRequest(
                    "API integration task",
                    "Task created through HTTP.",
                    TaskPriority.High,
                    null),
                cancellationToken);

        Assert.Equal(
            HttpStatusCode.Created,
            createTaskResponse.StatusCode);

        var createdTask =
            await createTaskResponse.Content
                .ReadFromJsonAsync<CreateTaskResponse>(
                    cancellationToken);

        Assert.NotNull(createdTask);

        Assert.Equal(
            new Uri(
                client.BaseAddress!,
                $"/api/projects/{project.ProjectId}/tasks/{createdTask.TaskItemId}"),
            createTaskResponse.Headers.Location);

        Assert.Equal(
            project.ProjectId,
            createdTask.ProjectId);

        Assert.Equal(
            TaskItemStatus.Backlog,
            createdTask.Status);

        Assert.Equal(
            TaskPriority.High,
            createdTask.Priority);

        Assert.Null(
            createdTask.AssigneeId);

        var getTaskResponse =
            await client.GetAsync(
                $"/api/projects/{project.ProjectId}/tasks/{createdTask.TaskItemId}",
                cancellationToken);

        Assert.Equal(
            HttpStatusCode.OK,
            getTaskResponse.StatusCode);

        var persistedTask =
            await getTaskResponse.Content
                .ReadFromJsonAsync<GetTaskByIdResponse>(
                    cancellationToken);

        Assert.NotNull(persistedTask);

        Assert.Equal(
            createdTask.TaskItemId,
            persistedTask.TaskItemId);

        Assert.Equal(
            project.ProjectId,
            persistedTask.ProjectId);

        Assert.Equal(
            "API integration task",
            persistedTask.Title);

        Assert.Equal(
            "Task created through HTTP.",
            persistedTask.Description);

        Assert.Equal(
            TaskItemStatus.Backlog,
            persistedTask.Status);

        Assert.Equal(
            TaskPriority.High,
            persistedTask.Priority);
    }

    [Fact]
    public async Task TaskStatusWorkflowCompletesAndPersistsTask()
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

        var createTaskResponse =
            await client.PostAsJsonAsync(
                $"/api/projects/{project.ProjectId}/tasks",
                new CreateTaskRequest(
                    "Workflow task",
                    null,
                    TaskPriority.Medium,
                    null),
                cancellationToken);

        var createdTask =
            await createTaskResponse.Content
                .ReadFromJsonAsync<CreateTaskResponse>(
                    cancellationToken);

        Assert.NotNull(createdTask);

        var transitions =
            new[]
            {
                TaskItemStatus.Todo,
                TaskItemStatus.InProgress,
                TaskItemStatus.Review,
                TaskItemStatus.Completed
            };

        ChangeTaskStatusResponse? lastResponse =
            null;

        foreach (var status in transitions)
        {
            var statusResponse =
                await client.PutAsJsonAsync(
                    $"/api/projects/{project.ProjectId}/tasks/{createdTask.TaskItemId}/status",
                    new ChangeTaskStatusRequest(
                        status),
                    cancellationToken);

            Assert.Equal(
                HttpStatusCode.OK,
                statusResponse.StatusCode);

            lastResponse =
                await statusResponse.Content
                    .ReadFromJsonAsync<ChangeTaskStatusResponse>(
                        cancellationToken);

            Assert.NotNull(lastResponse);

            Assert.Equal(
                status,
                lastResponse.Status);
        }

        Assert.NotNull(lastResponse);

        Assert.Equal(
            TaskItemStatus.Completed,
            lastResponse.Status);

        Assert.NotNull(
            lastResponse.CompletedAtUtc);

        var getTaskResponse =
            await client.GetAsync(
                $"/api/projects/{project.ProjectId}/tasks/{createdTask.TaskItemId}",
                cancellationToken);

        var persistedTask =
            await getTaskResponse.Content
                .ReadFromJsonAsync<GetTaskByIdResponse>(
                    cancellationToken);

        Assert.NotNull(persistedTask);

        Assert.Equal(
            TaskItemStatus.Completed,
            persistedTask.Status);

        Assert.NotNull(
            persistedTask.CompletedAtUtc);

        Assert.Equal(
            TruncateToMicroseconds(
                lastResponse.CompletedAtUtc.Value),
            persistedTask.CompletedAtUtc.Value);
    }

    private static DateTimeOffset TruncateToMicroseconds(
        DateTimeOffset value)
    {
        const long ticksPerMicrosecond = 10;

        var truncatedTicks =
            value.Ticks -
            value.Ticks % ticksPerMicrosecond;

        return new DateTimeOffset(
            truncatedTicks,
            value.Offset);
    }

    private async Task<HttpClient>
        CreateAuthenticatedClientAsync(
            CancellationToken cancellationToken)
    {
        var client =
            _fixture.CreateClient();

        var email =
            $"project-task-{Guid.NewGuid():N}@example.com";

        const string password =
            "StrongPassword123";

        var registerResponse =
            await client.PostAsJsonAsync(
                "/api/auth/register",
                new RegisterRequest(
                    email,
                    "Project Task API User",
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

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);

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
                    "API Integration Project",
                    "Created through the real HTTP pipeline."),
                cancellationToken);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var project =
            await response.Content
                .ReadFromJsonAsync<CreateProjectResponse>(
                    cancellationToken);

        Assert.NotNull(project);

        Assert.Equal(
            new Uri(
                client.BaseAddress!,
                $"/api/projects/{project.ProjectId}"),
            response.Headers.Location);

        return project;
    }
}
