using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Api.Contracts.Auth;
using TaskManager.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace TaskManager.Api.IntegrationTests;

[Collection(ApiIntegrationCollectionDefinition.Name)]
public sealed class AuthenticationSecurityTests
{
    private readonly ApiIntegrationFixture _fixture;

    public AuthenticationSecurityTests(
        ApiIntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task InvalidPasswordAndUnknownEmailUseSameUnauthorizedResponse()
    {
        var cancellationToken =
            TestContext.Current.CancellationToken;

        await _fixture.ResetDatabaseAsync(
            cancellationToken);

        using var client =
            _fixture.CreateClient();

        var email =
            $"auth-security-{Guid.NewGuid():N}@example.com";

        const string password =
            "StrongPassword123";

        var registerResponse =
            await client.PostAsJsonAsync(
                "/api/auth/register",
                new RegisterRequest(
                    email,
                    "Security User",
                    password),
                cancellationToken);

        Assert.Equal(
            HttpStatusCode.Created,
            registerResponse.StatusCode);

        var wrongPasswordResponse =
            await client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest(
                    email,
                    "WrongPassword123"),
                cancellationToken);

        var unknownEmailResponse =
            await client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest(
                    $"missing-{Guid.NewGuid():N}@example.com",
                    password),
                cancellationToken);

        await AssertInvalidCredentialsAsync(
            wrongPasswordResponse,
            cancellationToken);

        await AssertInvalidCredentialsAsync(
            unknownEmailResponse,
            cancellationToken);
    }

    private static async Task AssertInvalidCredentialsAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        var problem =
            await response.Content
                .ReadFromJsonAsync<ProblemDetails>(
                    cancellationToken);

        Assert.NotNull(problem);

        Assert.Equal(
            StatusCodes.Status401Unauthorized,
            problem.Status);

        Assert.Equal(
            "Unauthorized",
            problem.Title);

        Assert.Equal(
            "Invalid email or password.",
            problem.Detail);
    }
}
