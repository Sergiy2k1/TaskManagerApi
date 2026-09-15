using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace TaskManager.Api.Observability;

public static class OpenTelemetryExtensions
{
    private const string DefaultServiceName =
        "TaskManager.Api";

    private const string ServiceNameConfigurationKey =
        "OpenTelemetry:ServiceName";

    private const string OtlpEndpointConfigurationKey =
        "OTEL_EXPORTER_OTLP_ENDPOINT";

    public static IServiceCollection AddTaskManagerOpenTelemetry(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var serviceName =
            configuration[
                ServiceNameConfigurationKey];

        if (string.IsNullOrWhiteSpace(serviceName))
        {
            serviceName =
                DefaultServiceName;
        }

        var otlpEndpoint =
            GetOtlpEndpoint(configuration);

        services
            .AddOpenTelemetry()
            .ConfigureResource(
                resource =>
                    resource.AddService(
                        serviceName))
            .WithTracing(
                tracing =>
                {
                    tracing.AddAspNetCoreInstrumentation(
                        options =>
                        {
                            options.Filter =
                                httpContext =>
                                    !httpContext.Request.Path
                                        .StartsWithSegments(
                                            "/health");
                        });

                    if (otlpEndpoint is not null)
                    {
                        tracing.AddOtlpExporter(
                            options =>
                            {
                                options.Endpoint =
                                    otlpEndpoint;
                            });
                    }
                })
            .WithMetrics(
                metrics =>
                {
                    metrics
                        .AddAspNetCoreInstrumentation()
                        .AddRuntimeInstrumentation();

                    if (otlpEndpoint is not null)
                    {
                        metrics.AddOtlpExporter(
                            options =>
                            {
                                options.Endpoint =
                                    otlpEndpoint;
                            });
                    }
                });

        return services;
    }

    private static Uri? GetOtlpEndpoint(
        IConfiguration configuration)
    {
        var endpoint =
            configuration[
                OtlpEndpointConfigurationKey];

        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return null;
        }

        if (!Uri.TryCreate(
                endpoint,
                UriKind.Absolute,
                out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp &&
             uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException(
                $"{OtlpEndpointConfigurationKey} must be an absolute HTTP or HTTPS URI.");
        }

        return uri;
    }
}
