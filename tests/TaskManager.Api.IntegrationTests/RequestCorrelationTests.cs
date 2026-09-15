using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Api.Contracts.Projects;
using TaskManager.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace TaskManager.Api.IntegrationTests;

[Collection(ApiIntegrationCollectionDefinition.Name)]
public sealed class RequestCorrelationTests
{
    private const string CorrelationHeader =
        "X-Correlation-ID";

    private readonly ApiIntegrationFixture _fixture;

    public RequestCorrelationTests(
        ApiIntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task RequestWithoutCorrelationIdGetsGeneratedResponseHeader()
    {
        using var client =
            _fixture.CreateClient();

        var response =
            await client.GetAsync(
                "/api/ping",
                TestContext.Current.CancellationToken);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.True(
            response.Headers.TryGetValues(
                CorrelationHeader,
                out var values));

        var correlationId =
            Assert.Single(values);

        Assert.False(
            string.IsNullOrWhiteSpace(
                correlationId));

        Assert.True(
            correlationId.Length <= 64);
    }

    [Fact]
    public async Task ProblemDetailsUsesClientCorrelationIdAndTraceId()
    {
        var cancellationToken =
            TestContext.Current.CancellationToken;

        await _fixture.ResetDatabaseAsync(
            cancellationToken);

        using var client =
            _fixture.CreateClient();

        const string correlationId =
            "portfolio-request-123";

        using var request =
            new HttpRequestMessage(
                HttpMethod.Post,
                "/api/projects");

        request.Headers.Add(
            CorrelationHeader,
            correlationId);

        request.Content =
            JsonContent.Create(
                new CreateProjectRequest(
                    " ",
                    null));

        var response =
            await client.SendAsync(
                request,
                cancellationToken);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        Assert.True(
            response.Headers.TryGetValues(
                CorrelationHeader,
                out var values));

        Assert.Equal(
            correlationId,
            Assert.Single(values));
    }
}
