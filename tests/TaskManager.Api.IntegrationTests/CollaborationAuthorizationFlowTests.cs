using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Api.Contracts.Auth;
using TaskManager.Api.Contracts.Projects;
using TaskManager.Api.Contracts.TaskComments;
using TaskManager.Api.Contracts.Tasks;
using TaskManager.Api.IntegrationTests.Infrastructure;
using TaskManager.Domain.Enums;
using Xunit;

namespace TaskManager.Api.IntegrationTests;

[Collection(ApiIntegrationCollectionDefinition.Name)]
public sealed class CollaborationAuthorizationFlowTests
{
    private readonly ApiIntegrationFixture _fixture;

    public CollaborationAuthorizationFlowTests(
        ApiIntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task MemberRoleManagementAndRemovalFlowWorks()
    {
        var cancellationToken =
            TestContext.Current.CancellationToken;

        await _fixture.ResetDatabaseAsync(
            cancellationToken);

        var owner =
            await CreateAuthenticatedUserAsync(
                "Project Owner",
                cancellationToken);

        var manager =
            await CreateAuthenticatedUserAsync(
                "Future Manager",
                cancellationToken);

        var member =
            await CreateAuthenticatedUserAsync(
                "Project Member",
                cancellationToken);

        using var ownerClient = owner.Client;
        using var managerClient = manager.Client;
        using var memberClient = member.Client;

        var project =
            await CreateProjectAsync(
                ownerClient,
                cancellationToken);

        var initialManagerMembership =
            await AddMemberAsync(
                ownerClient,
                project.ProjectId,
                manager.Email,
                ProjectMemberRole.Member,
                cancellationToken);

        Assert.Equal(
            ProjectMemberRole.Member,
            initialManagerMembership.Role);

        var changeRoleResponse =
            await ownerClient.PatchAsJsonAsync(
                $"/api/projects/{project.ProjectId}/members/{manager.UserId}/role",
                new ChangeProjectMemberRoleRequest(
                    ProjectMemberRole.Manager),
                cancellationToken);

        Assert.Equal(
            HttpStatusCode.OK,
            changeRoleResponse.StatusCode);

        var promotedManager =
            await changeRoleResponse.Content
                .ReadFromJsonAsync<ChangeProjectMemberRoleResponse>(
                    cancellationToken);

        Assert.NotNull(promotedManager);

        Assert.Equal(
            ProjectMemberRole.Manager,
            promotedManager.Role);

        var memberAddedByManager =
            await AddMemberAsync(
                managerClient,
                project.ProjectId,
                member.Email,
                ProjectMemberRole.Member,
                cancellationToken);

        Assert.Equal(
            member.UserId,
            memberAddedByManager.UserId);

        var removeResponse =
            await managerClient.DeleteAsync(
                $"/api/projects/{project.ProjectId}/members/{member.UserId}",
                cancellationToken);

        Assert.Equal(
            HttpStatusCode.NoContent,
            removeResponse.StatusCode);

        var removedMemberReadResponse =
            await memberClient.GetAsync(
                $"/api/projects/{project.ProjectId}",
                cancellationToken);

        Assert.Equal(
            HttpStatusCode.NotFound,
            removedMemberReadResponse.StatusCode);

        await AssertProblemDetailsAsync(
            removedMemberReadResponse,
            HttpStatusCode.NotFound,
            "Not Found",
            "Project was not found.",
            cancellationToken);
    }

    [Fact]
    public async Task RegularMemberCannotManageMembersReturnsForbiddenProblemDetails()
    {
        var cancellationToken =
            TestContext.Current.CancellationToken;

        await _fixture.ResetDatabaseAsync(
            cancellationToken);

        var owner =
            await CreateAuthenticatedUserAsync(
                "Project Owner",
                cancellationToken);

        var member =
            await CreateAuthenticatedUserAsync(
                "Regular Member",
                cancellationToken);

        var targetUser =
            await CreateAuthenticatedUserAsync(
                "Target User",
                cancellationToken);

        using var ownerClient = owner.Client;
        using var memberClient = member.Client;
        using var targetClient = targetUser.Client;

        var project =
            await CreateProjectAsync(
                ownerClient,
                cancellationToken);

        await AddMemberAsync(
            ownerClient,
            project.ProjectId,
            member.Email,
            ProjectMemberRole.Member,
            cancellationToken);

        var response =
            await memberClient.PostAsJsonAsync(
                $"/api/projects/{project.ProjectId}/members",
                new AddProjectMemberRequest(
                    targetUser.Email,
                    ProjectMemberRole.Member),
                cancellationToken);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.Forbidden,
            "Forbidden",
            "You do not have permission to manage project members.",
            cancellationToken);
    }

    [Fact]
    public async Task AssignThenUnassignTaskPersistsAssignee()
    {
        var cancellationToken =
            TestContext.Current.CancellationToken;

        await _fixture.ResetDatabaseAsync(
            cancellationToken);

        var owner =
            await CreateAuthenticatedUserAsync(
                "Task Owner",
                cancellationToken);

        var member =
            await CreateAuthenticatedUserAsync(
                "Task Assignee",
                cancellationToken);

        using var ownerClient = owner.Client;
        using var memberClient = member.Client;

        var project =
            await CreateProjectAsync(
                ownerClient,
                cancellationToken);

        await AddMemberAsync(
            ownerClient,
            project.ProjectId,
            member.Email,
            ProjectMemberRole.Member,
            cancellationToken);

        var task =
            await CreateTaskAsync(
                ownerClient,
                project.ProjectId,
                cancellationToken);

        var assignResponse =
            await ownerClient.PutAsJsonAsync(
                $"/api/projects/{project.ProjectId}/tasks/{task.TaskItemId}/assignee",
                new AssignTaskRequest(
                    member.UserId),
                cancellationToken);

        Assert.Equal(
            HttpStatusCode.OK,
            assignResponse.StatusCode);

        var assignment =
            await assignResponse.Content
                .ReadFromJsonAsync<AssignTaskResponse>(
                    cancellationToken);

        Assert.NotNull(assignment);

        Assert.Equal(
            member.UserId,
            assignment.AssigneeId);

        var memberReadResponse =
            await memberClient.GetAsync(
                $"/api/projects/{project.ProjectId}/tasks/{task.TaskItemId}",
                cancellationToken);

        Assert.Equal(
            HttpStatusCode.OK,
            memberReadResponse.StatusCode);

        var assignedTask =
            await memberReadResponse.Content
                .ReadFromJsonAsync<GetTaskByIdResponse>(
                    cancellationToken);

        Assert.NotNull(assignedTask);

        Assert.Equal(
            member.UserId,
            assignedTask.AssigneeId);

        var unassignResponse =
            await memberClient.DeleteAsync(
                $"/api/projects/{project.ProjectId}/tasks/{task.TaskItemId}/assignee",
                cancellationToken);

        Assert.Equal(
            HttpStatusCode.NoContent,
            unassignResponse.StatusCode);

        var ownerReadResponse =
            await ownerClient.GetAsync(
                $"/api/projects/{project.ProjectId}/tasks/{task.TaskItemId}",
                cancellationToken);

        var unassignedTask =
            await ownerReadResponse.Content
                .ReadFromJsonAsync<GetTaskByIdResponse>(
                    cancellationToken);

        Assert.NotNull(unassignedTask);

        Assert.Null(
            unassignedTask.AssigneeId);
    }

    [Fact]
    public async Task CommentAuthorCanEditAndOwnerCanDeleteComment()
    {
        var cancellationToken =
            TestContext.Current.CancellationToken;

        await _fixture.ResetDatabaseAsync(
            cancellationToken);

        var owner =
            await CreateAuthenticatedUserAsync(
                "Comment Project Owner",
                cancellationToken);

        var member =
            await CreateAuthenticatedUserAsync(
                "Comment Author",
                cancellationToken);

        using var ownerClient = owner.Client;
        using var memberClient = member.Client;

        var project =
            await CreateProjectAsync(
                ownerClient,
                cancellationToken);

        await AddMemberAsync(
            ownerClient,
            project.ProjectId,
            member.Email,
            ProjectMemberRole.Member,
            cancellationToken);

        var task =
            await CreateTaskAsync(
                ownerClient,
                project.ProjectId,
                cancellationToken);

        var addCommentResponse =
            await memberClient.PostAsJsonAsync(
                $"/api/projects/{project.ProjectId}/tasks/{task.TaskItemId}/comments",
                new AddTaskCommentRequest(
                    "Initial API integration comment."),
                cancellationToken);

        Assert.Equal(
            HttpStatusCode.Created,
            addCommentResponse.StatusCode);

        var createdComment =
            await addCommentResponse.Content
                .ReadFromJsonAsync<TaskCommentResponse>(
                    cancellationToken);

        Assert.NotNull(createdComment);

        Assert.Equal(
            member.UserId,
            createdComment.AuthorUserId);

        var editResponse =
            await memberClient.PatchAsJsonAsync(
                $"/api/projects/{project.ProjectId}/tasks/{task.TaskItemId}/comments/{createdComment.CommentId}",
                new EditTaskCommentRequest(
                    "Edited API integration comment."),
                cancellationToken);

        Assert.Equal(
            HttpStatusCode.OK,
            editResponse.StatusCode);

        var editedComment =
            await editResponse.Content
                .ReadFromJsonAsync<TaskCommentResponse>(
                    cancellationToken);

        Assert.NotNull(editedComment);

        Assert.Equal(
            "Edited API integration comment.",
            editedComment.Content);

        Assert.NotNull(
            editedComment.UpdatedAtUtc);

        var deleteResponse =
            await ownerClient.DeleteAsync(
                $"/api/projects/{project.ProjectId}/tasks/{task.TaskItemId}/comments/{createdComment.CommentId}",
                cancellationToken);

        Assert.Equal(
            HttpStatusCode.NoContent,
            deleteResponse.StatusCode);

        var listResponse =
            await memberClient.GetAsync(
                $"/api/projects/{project.ProjectId}/tasks/{task.TaskItemId}/comments",
                cancellationToken);

        Assert.Equal(
            HttpStatusCode.OK,
            listResponse.StatusCode);

        var comments =
            await listResponse.Content
                .ReadFromJsonAsync<List<TaskCommentResponse>>(
                    cancellationToken);

        Assert.NotNull(comments);
        Assert.Empty(comments);
    }

    [Fact]
    public async Task OutsiderCannotSeeProjectReturnsNotFoundProblemDetails()
    {
        var cancellationToken =
            TestContext.Current.CancellationToken;

        await _fixture.ResetDatabaseAsync(
            cancellationToken);

        var owner =
            await CreateAuthenticatedUserAsync(
                "Project Owner",
                cancellationToken);

        var outsider =
            await CreateAuthenticatedUserAsync(
                "Outsider",
                cancellationToken);

        using var ownerClient = owner.Client;
        using var outsiderClient = outsider.Client;

        var project =
            await CreateProjectAsync(
                ownerClient,
                cancellationToken);

        var response =
            await outsiderClient.GetAsync(
                $"/api/projects/{project.ProjectId}",
                cancellationToken);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.NotFound,
            "Not Found",
            "Project was not found.",
            cancellationToken);
    }

    [Fact]
    public async Task ArchivedProjectRejectsCommentMutationWithConflictProblemDetails()
    {
        var cancellationToken =
            TestContext.Current.CancellationToken;

        await _fixture.ResetDatabaseAsync(
            cancellationToken);

        var owner =
            await CreateAuthenticatedUserAsync(
                "Archived Project Owner",
                cancellationToken);

        using var ownerClient = owner.Client;

        var project =
            await CreateProjectAsync(
                ownerClient,
                cancellationToken);

        var task =
            await CreateTaskAsync(
                ownerClient,
                project.ProjectId,
                cancellationToken);

        var archiveResponse =
            await ownerClient.PostAsync(
                $"/api/projects/{project.ProjectId}/archive",
                content: null,
                cancellationToken);

        Assert.Equal(
            HttpStatusCode.OK,
            archiveResponse.StatusCode);

        var response =
            await ownerClient.PostAsJsonAsync(
                $"/api/projects/{project.ProjectId}/tasks/{task.TaskItemId}/comments",
                new AddTaskCommentRequest(
                    "This mutation must be rejected."),
                cancellationToken);

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.Conflict,
            "Conflict",
            "Cannot add comments to an archived project.",
            cancellationToken);
    }

    [Fact]
    public async Task InvalidProjectNameReturnsValidationProblemDetails()
    {
        var cancellationToken =
            TestContext.Current.CancellationToken;

        await _fixture.ResetDatabaseAsync(
            cancellationToken);

        var user =
            await CreateAuthenticatedUserAsync(
                "Validation User",
                cancellationToken);

        using var client = user.Client;

        var response =
            await client.PostAsJsonAsync(
                "/api/projects",
                new CreateProjectRequest(
                    " ",
                    null),
                cancellationToken);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.BadRequest,
            "Validation error",
            "Project name cannot be empty.",
            cancellationToken);
    }

    private async Task<AuthenticatedUser>
        CreateAuthenticatedUserAsync(
            string displayName,
            CancellationToken cancellationToken)
    {
        var client =
            _fixture.CreateClient();

        var email =
            $"collaboration-{Guid.NewGuid():N}@example.com";

        const string password =
            "StrongPassword123";

        var registerResponse =
            await client.PostAsJsonAsync(
                "/api/auth/register",
                new RegisterRequest(
                    email,
                    displayName,
                    password),
                cancellationToken);

        Assert.Equal(
            HttpStatusCode.Created,
            registerResponse.StatusCode);

        var registeredUser =
            await registerResponse.Content
                .ReadFromJsonAsync<RegisterResponse>(
                    cancellationToken);

        Assert.NotNull(registeredUser);

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

        return new AuthenticatedUser(
            client,
            registeredUser.UserId,
            email);
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
                    "Collaboration API Project",
                    "Authorization and collaboration integration coverage."),
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

    private static async Task<CreateTaskResponse>
        CreateTaskAsync(
            HttpClient client,
            Guid projectId,
            CancellationToken cancellationToken)
    {
        var response =
            await client.PostAsJsonAsync(
                $"/api/projects/{projectId}/tasks",
                new CreateTaskRequest(
                    "Collaboration integration task",
                    null,
                    TaskPriority.Medium,
                    null),
                cancellationToken);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var task =
            await response.Content
                .ReadFromJsonAsync<CreateTaskResponse>(
                    cancellationToken);

        Assert.NotNull(task);

        return task;
    }

    private static async Task<AddProjectMemberResponse>
        AddMemberAsync(
            HttpClient client,
            Guid projectId,
            string email,
            ProjectMemberRole role,
            CancellationToken cancellationToken)
    {
        var response =
            await client.PostAsJsonAsync(
                $"/api/projects/{projectId}/members",
                new AddProjectMemberRequest(
                    email,
                    role),
                cancellationToken);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var member =
            await response.Content
                .ReadFromJsonAsync<AddProjectMemberResponse>(
                    cancellationToken);

        Assert.NotNull(member);

        return member;
    }

    private static async Task AssertProblemDetailsAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus,
        string expectedTitle,
        string expectedDetail,
        CancellationToken cancellationToken)
    {
        var problem =
            await response.Content
                .ReadFromJsonAsync<ProblemDetails>(
                    cancellationToken);

        Assert.NotNull(problem);

        Assert.Equal(
            (int)expectedStatus,
            problem.Status);

        Assert.Equal(
            expectedTitle,
            problem.Title);

        Assert.Equal(
            expectedDetail,
            problem.Detail);

        Assert.Equal(
            response.RequestMessage?.RequestUri?.AbsolutePath,
            problem.Instance);

        Assert.True(
            problem.Extensions.ContainsKey(
                "traceId"));
    }

    private sealed record AuthenticatedUser(
        HttpClient Client,
        Guid UserId,
        string Email);
}
