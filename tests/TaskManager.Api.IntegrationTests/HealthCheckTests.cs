using System.Net;
using System.Text.Json;
using TaskManager.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace TaskManager.Api.IntegrationTests;

[Collection(ApiIntegrationCollectionDefinition.Name)]
public sealed class HealthCheckTests
{
    private readonly ApiIntegrationFixture _fixture;

    public HealthCheckTests(
        ApiIntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task LivenessDoesNotDependOnDatabaseChecks()
    {
        using var client =
            _fixture.CreateClient();

        var response =
            await client.GetAsync(
                "/health/live",
                TestContext.Current.CancellationToken);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        using var document =
            await JsonDocument.ParseAsync(
                await response.Content
                    .ReadAsStreamAsync(
                        TestContext.Current.CancellationToken),
                cancellationToken:
                    TestContext.Current.CancellationToken);

        var root =
            document.RootElement;

        Assert.Equal(
            "Healthy",
            root.GetProperty("status").GetString());

        Assert.Equal(
            0,
            root.GetProperty("checks").GetArrayLength());
    }

    [Fact]
    public async Task ReadinessChecksPostgreSqlConnectivity()
    {
        using var client =
            _fixture.CreateClient();

        var response =
            await client.GetAsync(
                "/health/ready",
                TestContext.Current.CancellationToken);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        using var document =
            await JsonDocument.ParseAsync(
                await response.Content
                    .ReadAsStreamAsync(
                        TestContext.Current.CancellationToken),
                cancellationToken:
                    TestContext.Current.CancellationToken);

        var root =
            document.RootElement;

        Assert.Equal(
            "Healthy",
            root.GetProperty("status").GetString());

        var checks =
            root.GetProperty("checks");

        Assert.Equal(
            1,
            checks.GetArrayLength());

        var databaseCheck =
            checks[0];

        Assert.Equal(
            "postgresql",
            databaseCheck.GetProperty("name").GetString());

        Assert.Equal(
            "Healthy",
            databaseCheck.GetProperty("status").GetString());
    }
}
