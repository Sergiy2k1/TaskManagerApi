using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Api.Contracts;
using TaskManager.Api.Contracts.Auth;
using TaskManager.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace TaskManager.Api.IntegrationTests;

[Collection(ApiIntegrationCollectionDefinition.Name)]
public sealed class AuthenticationFlowTests
{
    private readonly ApiIntegrationFixture _fixture;

    public AuthenticationFlowTests(
        ApiIntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task PingReturnsOk()
    {
        using var client =
            _fixture.CreateClient();

        var cancellationToken =
            TestContext.Current.CancellationToken;

        var response =
            await client.GetAsync(
                "/api/ping",
                cancellationToken);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var body =
            await response.Content
                .ReadFromJsonAsync<PingResponse>(
                    cancellationToken);

        Assert.NotNull(body);

        Assert.Equal(
            "TaskManager API is running.",
            body.Message);
    }

    [Fact]
    public async Task ProfileWithoutTokenReturnsUnauthorized()
    {
        using var client =
            _fixture.CreateClient();

        var response =
            await client.GetAsync(
                "/api/profile",
                TestContext.Current.CancellationToken);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        Assert.Contains(
            response.Headers.WwwAuthenticate,
            header =>
                string.Equals(
                    header.Scheme,
                    "Bearer",
                    StringComparison.OrdinalIgnoreCase));

        var problem =
            await response.Content
                .ReadFromJsonAsync<ProblemDetails>(
                    TestContext.Current.CancellationToken);

        Assert.NotNull(problem);
        Assert.Equal(
            StatusCodes.Status401Unauthorized,
            problem.Status);
        Assert.Equal(
            "Unauthorized",
            problem.Title);
        Assert.Equal(
            "Authentication is required.",
            problem.Detail);
        Assert.True(
            problem.Extensions.ContainsKey(
                "traceId"));
        Assert.True(
            problem.Extensions.ContainsKey(
                "correlationId"));
    }

    [Fact]
    public async Task ProfileWithInvalidTokenReturnsGenericUnauthorizedProblemDetails()
    {
        using var client =
            _fixture.CreateClient();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                "not-a-valid-jwt");

        var response =
            await client.GetAsync(
                "/api/profile",
                TestContext.Current.CancellationToken);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        var problem =
            await response.Content
                .ReadFromJsonAsync<ProblemDetails>(
                    TestContext.Current.CancellationToken);

        Assert.NotNull(problem);
        Assert.Equal(
            "Authentication is required.",
            problem.Detail);

        Assert.DoesNotContain(
            "token",
            problem.Detail,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RegisterLoginAndProfileFlowReturnsAuthenticatedUser()
    {
        var cancellationToken =
            TestContext.Current.CancellationToken;

        await _fixture.ResetDatabaseAsync(
            cancellationToken);

        using var client =
            _fixture.CreateClient();

        var email =
            $"api-user-{Guid.NewGuid():N}@example.com";

        const string displayName =
            "API Integration User";

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
        Assert.NotEqual(
            Guid.Empty,
            registeredUser.UserId);

        Assert.Equal(
            email,
            registeredUser.Email);

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

        Assert.True(
            loginResponse.Headers.CacheControl?.NoStore);

        var login =
            await loginResponse.Content
                .ReadFromJsonAsync<LoginResponse>(
                    cancellationToken);

        Assert.NotNull(login);
        Assert.Equal(
            registeredUser.UserId,
            login.UserId);

        Assert.False(
            string.IsNullOrWhiteSpace(
                login.AccessToken));

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                login.AccessToken);

        var profileResponse =
            await client.GetAsync(
                "/api/profile",
                cancellationToken);

        Assert.Equal(
            HttpStatusCode.OK,
            profileResponse.StatusCode);

        var profile =
            await profileResponse.Content
                .ReadFromJsonAsync<ProfileResponse>(
                    cancellationToken);

        Assert.NotNull(profile);

        Assert.Equal(
            registeredUser.UserId,
            profile.UserId);

        Assert.Equal(
            email,
            profile.Email);

        Assert.Equal(
            displayName,
            profile.DisplayName);
    }

    private sealed record ProfileResponse(
        Guid UserId,
        string? Email,
        string? DisplayName);
}
