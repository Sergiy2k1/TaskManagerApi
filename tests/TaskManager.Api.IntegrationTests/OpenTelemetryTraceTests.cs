using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Api.Contracts.Auth;
using TaskManager.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace TaskManager.Api.IntegrationTests;

[Collection(ApiIntegrationCollectionDefinition.Name)]
public sealed class OpenTelemetryTraceTests
{
    private readonly ApiIntegrationFixture _fixture;

    public OpenTelemetryTraceTests(
        ApiIntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task IncomingTraceParentFlowsIntoProblemDetailsTraceId()
    {
        var cancellationToken =
            TestContext.Current.CancellationToken;

        await _fixture.ResetDatabaseAsync(
            cancellationToken);

        using var client =
            _fixture.CreateClient();

        var email =
            $"trace-{Guid.NewGuid():N}@example.com";

        var registerRequest =
            new RegisterRequest(
                email,
                "Trace User",
                "StrongPassword123");

        var firstResponse =
            await client.PostAsJsonAsync(
                "/api/auth/register",
                registerRequest,
                cancellationToken);

        Assert.Equal(
            HttpStatusCode.Created,
            firstResponse.StatusCode);

        const string traceId =
            "4bf92f3577b34da6a3ce929d0e0e4736";

        const string parentSpanId =
            "00f067aa0ba902b7";

        using var secondRequest =
            new HttpRequestMessage(
                HttpMethod.Post,
                "/api/auth/register");

        secondRequest.Headers.TryAddWithoutValidation(
            "traceparent",
            $"00-{traceId}-{parentSpanId}-01");

        secondRequest.Content =
            JsonContent.Create(
                registerRequest);

        var secondResponse =
            await client.SendAsync(
                secondRequest,
                cancellationToken);

        Assert.Equal(
            HttpStatusCode.Conflict,
            secondResponse.StatusCode);

        var problem =
            await secondResponse.Content
                .ReadFromJsonAsync<ProblemDetails>(
                    cancellationToken);

        Assert.NotNull(problem);

        Assert.True(
            problem.Extensions.TryGetValue(
                "traceId",
                out var traceIdValue));

        var traceIdElement =
            Assert.IsType<JsonElement>(
                traceIdValue);

        Assert.Equal(
            traceId,
            traceIdElement.GetString());
    }
}
