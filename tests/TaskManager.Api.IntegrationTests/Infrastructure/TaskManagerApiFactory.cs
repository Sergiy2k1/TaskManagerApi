using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TaskManager.Api.IntegrationTests.Infrastructure;

public sealed class TaskManagerApiFactory
    : WebApplicationFactory<Program>
{
    private const string JwtIssuer =
        "TaskManager.Api.IntegrationTests";

    private const string JwtAudience =
        "TaskManager.Api.IntegrationTests.Client";

    private const string JwtSigningKey =
        "VGFza01hbmFnZXItYXBpLWludGVncmF0aW9uLXRlc3RzLXNpZ25pbmcta2V5LTIwMjY=";

    public TaskManagerApiFactory(
        string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            connectionString);

        // Program reads the database connection string and JWT settings
        // immediately after WebApplication.CreateBuilder(args). Settings
        // added later in ConfigureWebHost are therefore too late for this
        // minimal-host bootstrap path. Environment variables are available
        // to CreateBuilder from the start and keep production code unchanged.
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__Database",
            connectionString);

        Environment.SetEnvironmentVariable(
            "Jwt__Issuer",
            JwtIssuer);

        Environment.SetEnvironmentVariable(
            "Jwt__Audience",
            JwtAudience);

        Environment.SetEnvironmentVariable(
            "Jwt__SigningKey",
            JwtSigningKey);

        Environment.SetEnvironmentVariable(
            "Jwt__AccessTokenLifetimeMinutes",
            "15");
    }

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }
}
