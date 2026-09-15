using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
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
