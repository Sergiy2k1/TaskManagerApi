using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TaskManager.Api.Contracts.Auth;
using TaskManager.Api.Contracts.Common;
using TaskManager.Api.Contracts.Projects;
using TaskManager.Api.Contracts.TaskComments;
using TaskManager.Api.Contracts.Tasks;
using TaskManager.Api.IntegrationTests.Infrastructure;
using TaskManager.Domain.Enums;
using Xunit;

namespace TaskManager.Api.IntegrationTests;

[Collection(ApiIntegrationCollectionDefinition.Name)]
public sealed class CollectionPaginationTests
{
    private readonly ApiIntegrationFixture _fixture;

    public CollectionPaginationTests(ApiIntegrationFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task ProjectMemberAndCommentListsReturnPagedMetadata()
    {
        var ct = TestContext.Current.CancellationToken;
        await _fixture.ResetDatabaseAsync(ct);
        using var owner = await CreateAuthenticatedClientAsync(ct);

        await CreateProjectAsync(owner, "First Project", ct);
        await CreateProjectAsync(owner, "Second Project", ct);
        var project = await CreateProjectAsync(owner, "Third Project", ct);

        var projectsResponse = await owner.GetAsync("/api/projects?page=2&pageSize=2", ct);
        var projects = await projectsResponse.Content.ReadFromJsonAsync<PagedResponse<GetProjectResponse>>(ct);
        Assert.Equal(HttpStatusCode.OK, projectsResponse.StatusCode);
        Assert.NotNull(projects);
        Assert.Equal(3, projects.TotalCount);
        Assert.Equal(2, projects.TotalPages);
        Assert.Single(projects.Items);

        var firstMember = await RegisterUserAsync(_fixture.CreateClient(), ct);
        var secondMember = await RegisterUserAsync(_fixture.CreateClient(), ct);
        await AddMemberAsync(owner, project.ProjectId, firstMember.Email, ct);
        await AddMemberAsync(owner, project.ProjectId, secondMember.Email, ct);

        var membersResponse = await owner.GetAsync($"/api/projects/{project.ProjectId}/members?page=2&pageSize=2", ct);
        var members = await membersResponse.Content.ReadFromJsonAsync<PagedResponse<GetProjectMemberResponse>>(ct);
        Assert.Equal(HttpStatusCode.OK, membersResponse.StatusCode);
        Assert.NotNull(members);
        Assert.Equal(3, members.TotalCount);
        Assert.Equal(2, members.TotalPages);
        Assert.Single(members.Items);

        var task = await CreateTaskAsync(owner, project.ProjectId, ct);
        await AddCommentAsync(owner, project.ProjectId, task.TaskItemId, "First comment", ct);
        await AddCommentAsync(owner, project.ProjectId, task.TaskItemId, "Second comment", ct);
        await AddCommentAsync(owner, project.ProjectId, task.TaskItemId, "Third comment", ct);

        var commentsResponse = await owner.GetAsync($"/api/projects/{project.ProjectId}/tasks/{task.TaskItemId}/comments?page=2&pageSize=2", ct);
        var comments = await commentsResponse.Content.ReadFromJsonAsync<PagedResponse<TaskCommentResponse>>(ct);
        Assert.Equal(HttpStatusCode.OK, commentsResponse.StatusCode);
        Assert.NotNull(comments);
        Assert.Equal(3, comments.TotalCount);
        Assert.Equal(2, comments.TotalPages);
        Assert.Single(comments.Items);
        Assert.Equal("Third comment", comments.Items[0].Content);
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(CancellationToken ct)
    {
        var client = _fixture.CreateClient();
        var user = await RegisterUserAsync(client, ct);
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(user.Email, "StrongPassword123"), ct);
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>(ct);
        Assert.NotNull(login);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        return client;
    }

    private static async Task<RegisteredUser> RegisterUserAsync(HttpClient client, CancellationToken ct)
    {
        using var ownedClient = client;
        var email = $"pagination-{Guid.NewGuid():N}@example.com";
        var response = await ownedClient.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "Pagination User", "StrongPassword123"), ct);
        var registered = await response.Content.ReadFromJsonAsync<RegisterResponse>(ct);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(registered);
        return new RegisteredUser(registered.UserId, email);
    }

    private static async Task<CreateProjectResponse> CreateProjectAsync(HttpClient client, string name, CancellationToken ct)
    {
        var response = await client.PostAsJsonAsync("/api/projects", new CreateProjectRequest(name, null), ct);
        var project = await response.Content.ReadFromJsonAsync<CreateProjectResponse>(ct);
        Assert.NotNull(project);
        return project;
    }

    private static async Task AddMemberAsync(HttpClient client, Guid projectId, string email, CancellationToken ct)
    {
        var response = await client.PostAsJsonAsync($"/api/projects/{projectId}/members", new AddProjectMemberRequest(email, ProjectMemberRole.Member), ct);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static async Task<CreateTaskResponse> CreateTaskAsync(HttpClient client, Guid projectId, CancellationToken ct)
    {
        var response = await client.PostAsJsonAsync($"/api/projects/{projectId}/tasks", new CreateTaskRequest("Pagination task", null, TaskPriority.Medium, null), ct);
        var task = await response.Content.ReadFromJsonAsync<CreateTaskResponse>(ct);
        Assert.NotNull(task);
        return task;
    }

    private static async Task AddCommentAsync(HttpClient client, Guid projectId, Guid taskItemId, string content, CancellationToken ct)
    {
        var response = await client.PostAsJsonAsync($"/api/projects/{projectId}/tasks/{taskItemId}/comments", new AddTaskCommentRequest(content), ct);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private sealed record RegisteredUser(Guid UserId, string Email);
}
