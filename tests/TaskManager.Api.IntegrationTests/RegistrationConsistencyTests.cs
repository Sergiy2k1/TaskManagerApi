using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Api.Contracts.Auth;
using TaskManager.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace TaskManager.Api.IntegrationTests;

[Collection(ApiIntegrationCollectionDefinition.Name)]
public sealed class RegistrationConsistencyTests
{
    private readonly ApiIntegrationFixture _fixture;

    public RegistrationConsistencyTests(
        ApiIntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ConcurrentRegistrationWithSameEmailReturnsCreatedAndConflict()
    {
        var cancellationToken =
            TestContext.Current.CancellationToken;

        await _fixture.ResetDatabaseAsync(
            cancellationToken);

        using var firstClient =
            _fixture.CreateClient();

        using var secondClient =
            _fixture.CreateClient();

        var email =
            $"concurrent-register-{Guid.NewGuid():N}@example.com";

        var request =
            new RegisterRequest(
                email,
                "Concurrent User",
                "StrongPassword123");

        var firstRequest =
            firstClient.PostAsJsonAsync(
                "/api/auth/register",
                request,
                cancellationToken);

        var secondRequest =
            secondClient.PostAsJsonAsync(
                "/api/auth/register",
                request,
                cancellationToken);

        var responses =
            await Task.WhenAll(
                firstRequest,
                secondRequest);

        Assert.Single(
            responses,
            response =>
                response.StatusCode ==
                HttpStatusCode.Created);

        var conflictResponse =
            Assert.Single(
                responses,
                response =>
                    response.StatusCode ==
                    HttpStatusCode.Conflict);

        var problem =
            await conflictResponse.Content
                .ReadFromJsonAsync<ProblemDetails>(
                    cancellationToken);

        Assert.NotNull(problem);

        Assert.Equal(
            "A user with this email already exists.",
            problem.Detail);
    }
}
