using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

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

    private readonly string _connectionString;

    public TaskManagerApiFactory(
        string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration(
            (_, configurationBuilder) =>
            {
                var values =
                    new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:Database"] =
                            _connectionString,

                        ["Jwt:Issuer"] =
                            JwtIssuer,

                        ["Jwt:Audience"] =
                            JwtAudience,

                        ["Jwt:SigningKey"] =
                            JwtSigningKey,

                        ["Jwt:AccessTokenLifetimeMinutes"] =
                            "15"
                    };

                configurationBuilder
                    .AddInMemoryCollection(values);
            });
    }
}
