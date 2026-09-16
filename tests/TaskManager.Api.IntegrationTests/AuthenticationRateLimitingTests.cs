using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Api.Contracts.Auth;
using TaskManager.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace TaskManager.Api.IntegrationTests;

[Collection(ApiIntegrationCollectionDefinition.Name)]
public sealed class AuthenticationRateLimitingTests
{
    private readonly ApiIntegrationFixture _fixture;

    public AuthenticationRateLimitingTests(
        ApiIntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task LoginReturnsTooManyRequestsAfterPerClientLimit()
    {
        var cancellationToken =
            TestContext.Current.CancellationToken;

        await _fixture.ResetDatabaseAsync(
            cancellationToken);

        using var factory =
            _fixture.CreateRateLimitedFactory(
                loginPermitLimit: 2,
                registerPermitLimit: 100);

        using var client =
            factory.CreateClient();

        var request =
            new LoginRequest(
                "missing@example.com",
                "StrongPassword123");

        var firstResponse =
            await client.PostAsJsonAsync(
                "/api/auth/login",
                request,
                cancellationToken);

        var secondResponse =
            await client.PostAsJsonAsync(
                "/api/auth/login",
                request,
                cancellationToken);

        var limitedResponse =
            await client.PostAsJsonAsync(
                "/api/auth/login",
                request,
                cancellationToken);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            firstResponse.StatusCode);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            secondResponse.StatusCode);

        await AssertRateLimitedAsync(
            limitedResponse,
            cancellationToken);
    }

    [Fact]
    public async Task RegisterReturnsTooManyRequestsAfterPerClientLimit()
    {
        var cancellationToken =
            TestContext.Current.CancellationToken;

        await _fixture.ResetDatabaseAsync(
            cancellationToken);

        using var factory =
            _fixture.CreateRateLimitedFactory(
                loginPermitLimit: 100,
                registerPermitLimit: 2);

        using var client =
            factory.CreateClient();

        var firstResponse =
            await client.PostAsJsonAsync(
                "/api/auth/register",
                CreateRegisterRequest(),
                cancellationToken);

        var secondResponse =
            await client.PostAsJsonAsync(
                "/api/auth/register",
                CreateRegisterRequest(),
                cancellationToken);

        var limitedResponse =
            await client.PostAsJsonAsync(
                "/api/auth/register",
                CreateRegisterRequest(),
                cancellationToken);

        Assert.Equal(
            HttpStatusCode.Created,
            firstResponse.StatusCode);

        Assert.Equal(
            HttpStatusCode.Created,
            secondResponse.StatusCode);

        await AssertRateLimitedAsync(
            limitedResponse,
            cancellationToken);
    }

    private static RegisterRequest CreateRegisterRequest()
    {
        return new RegisterRequest(
            $"rate-limit-{Guid.NewGuid():N}@example.com",
            "Rate Limit User",
            "StrongPassword123");
    }

    private static async Task AssertRateLimitedAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        Assert.Equal(
            HttpStatusCode.TooManyRequests,
            response.StatusCode);

        Assert.True(
            response.Headers.TryGetValues(
                "Retry-After",
                out var retryAfterValues));

        Assert.False(
            string.IsNullOrWhiteSpace(
                Assert.Single(
                    retryAfterValues)));

        var problem =
            await response.Content
                .ReadFromJsonAsync<ProblemDetails>(
                    cancellationToken);

        Assert.NotNull(problem);

        Assert.Equal(
            StatusCodes.Status429TooManyRequests,
            problem.Status);

        Assert.Equal(
            "Too Many Requests",
            problem.Title);

        Assert.Equal(
            "Too many authentication requests. Try again later.",
            problem.Detail);

        Assert.True(
            problem.Extensions.ContainsKey(
                "traceId"));

        Assert.True(
            problem.Extensions.ContainsKey(
                "correlationId"));
    }
}
