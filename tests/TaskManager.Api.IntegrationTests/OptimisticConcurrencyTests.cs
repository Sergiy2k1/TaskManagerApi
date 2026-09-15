using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Api.Contracts.Auth;
using TaskManager.Api.Contracts.Projects;
using TaskManager.Api.Contracts.Tasks;
using TaskManager.Api.IntegrationTests.Infrastructure;
using TaskManager.Domain.Enums;
using Xunit;

namespace TaskManager.Api.IntegrationTests;

[Collection(ApiIntegrationCollectionDefinition.Name)]
public sealed class OptimisticConcurrencyTests
{
    private readonly ApiIntegrationFixture _fixture;

    public OptimisticConcurrencyTests(
        ApiIntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task StaleProjectVersionReturnsConflict()
    {
        var token = TestContext.Current.CancellationToken;
        await _fixture.ResetDatabaseAsync(token);

        using var client =
            await CreateAuthenticatedClientAsync(token);

        var project =
            await CreateProjectAsync(client, token);

        var current =
            await GetProjectAsync(
                client,
                project.ProjectId,
                token);

        Assert.Equal(1, current.Version);

        var updateResponse =
            await client.PutAsJsonAsync(
                $"/api/projects/{project.ProjectId}",
                new UpdateProjectRequest(
                    "Updated Project",
                    "First writer wins.",
                    current.Version),
                token);

        Assert.Equal(
            HttpStatusCode.OK,
            updateResponse.StatusCode);

        var updated =
            await updateResponse.Content
                .ReadFromJsonAsync<UpdateProjectResponse>(token);

        Assert.NotNull(updated);
        Assert.Equal(2, updated.Version);

        var staleResponse =
            await client.PutAsJsonAsync(
                $"/api/projects/{project.ProjectId}",
                new UpdateProjectRequest(
                    "Stale Project",
                    null,
                    current.Version),
                token);

        Assert.Equal(
            HttpStatusCode.Conflict,
            staleResponse.StatusCode);

        var problem =
            await staleResponse.Content
                .ReadFromJsonAsync<ProblemDetails>(token);

        Assert.NotNull(problem);
        Assert.Equal(
            "Project was modified since it was loaded. Reload the project and retry.",
            problem.Detail);
    }

    [Fact]
    public async Task StaleTaskVersionReturnsConflict()
    {
        var token = TestContext.Current.CancellationToken;
        await _fixture.ResetDatabaseAsync(token);

        using var client =
            await CreateAuthenticatedClientAsync(token);

        var project =
            await CreateProjectAsync(client, token);

        var createTaskResponse =
            await client.PostAsJsonAsync(
                $"/api/projects/{project.ProjectId}/tasks",
                new CreateTaskRequest(
                    "Concurrency Task",
                    null,
                    TaskPriority.Medium,
                    null),
                token);

        var createdTask =
            await createTaskResponse.Content
                .ReadFromJsonAsync<CreateTaskResponse>(token);

        Assert.NotNull(createdTask);

        var current =
            await GetTaskAsync(
                client,
                project.ProjectId,
                createdTask.TaskItemId,
                token);

        Assert.Equal(1, current.Version);

        var updateResponse =
            await client.PutAsJsonAsync(
                $"/api/projects/{project.ProjectId}/tasks/{createdTask.TaskItemId}",
                new UpdateTaskRequest(
                    "Updated Task",
                    "First writer wins.",
                    TaskPriority.High,
                    null,
                    current.Version),
                token);

        Assert.Equal(
            HttpStatusCode.OK,
            updateResponse.StatusCode);

        var updated =
            await updateResponse.Content
                .ReadFromJsonAsync<UpdateTaskResponse>(token);

        Assert.NotNull(updated);
        Assert.Equal(2, updated.Version);

        var staleResponse =
            await client.PutAsJsonAsync(
                $"/api/projects/{project.ProjectId}/tasks/{createdTask.TaskItemId}",
                new UpdateTaskRequest(
                    "Stale Task",
                    null,
                    TaskPriority.Low,
                    null,
                    current.Version),
                token);

        Assert.Equal(
            HttpStatusCode.Conflict,
            staleResponse.StatusCode);

        var problem =
            await staleResponse.Content
                .ReadFromJsonAsync<ProblemDetails>(token);

        Assert.NotNull(problem);
        Assert.Equal(
            "Task was modified since it was loaded. Reload the task and retry.",
            problem.Detail);
    }

    private async Task<HttpClient>
        CreateAuthenticatedClientAsync(
            CancellationToken token)
    {
        var client = _fixture.CreateClient();
        var email =
            $"concurrency-{Guid.NewGuid():N}@example.com";
        const string password = "StrongPassword123";

        var registerResponse =
            await client.PostAsJsonAsync(
                "/api/auth/register",
                new RegisterRequest(
                    email,
                    "Concurrency User",
                    password),
                token);

        Assert.Equal(
            HttpStatusCode.Created,
            registerResponse.StatusCode);

        var loginResponse =
            await client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest(
                    email,
                    password),
                token);

        var login =
            await loginResponse.Content
                .ReadFromJsonAsync<LoginResponse>(token);

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
            CancellationToken token)
    {
        var response =
            await client.PostAsJsonAsync(
                "/api/projects",
                new CreateProjectRequest(
                    "Concurrency Project",
                    null),
                token);

        var project =
            await response.Content
                .ReadFromJsonAsync<CreateProjectResponse>(token);

        Assert.NotNull(project);
        return project;
    }

    private static async Task<GetProjectByIdResponse>
        GetProjectAsync(
            HttpClient client,
            Guid projectId,
            CancellationToken token)
    {
        var response =
            await client.GetAsync(
                $"/api/projects/{projectId}",
                token);

        var project =
            await response.Content
                .ReadFromJsonAsync<GetProjectByIdResponse>(token);

        Assert.NotNull(project);
        return project;
    }

    private static async Task<GetTaskByIdResponse>
        GetTaskAsync(
            HttpClient client,
            Guid projectId,
            Guid taskItemId,
            CancellationToken token)
    {
        var response =
            await client.GetAsync(
                $"/api/projects/{projectId}/tasks/{taskItemId}",
                token);

        var task =
            await response.Content
                .ReadFromJsonAsync<GetTaskByIdResponse>(token);

        Assert.NotNull(task);
        return task;
    }
}
