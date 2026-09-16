using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace TaskManager.Api.RateLimiting;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddTaskManagerRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var section =
            configuration.GetSection(
                AuthenticationRateLimitOptions.SectionName);

        var settings =
            section.Get<AuthenticationRateLimitOptions>()
            ?? throw new InvalidOperationException(
                "Authentication rate-limit configuration was not found.");

        Validate(settings);

        services
            .AddOptions<AuthenticationRateLimitOptions>()
            .Bind(section)
            .Validate(
                options =>
                    options.LoginPermitLimit is >= 1 and <= 10_000,
                "Login rate-limit permit limit must be between 1 and 10000.")
            .Validate(
                options =>
                    options.RegisterPermitLimit is >= 1 and <= 10_000,
                "Register rate-limit permit limit must be between 1 and 10000.")
            .Validate(
                options =>
                    options.WindowSeconds is >= 1 and <= 3600,
                "Authentication rate-limit window must be between 1 and 3600 seconds.")
            .ValidateOnStart();

        services.AddRateLimiter(
            options =>
            {
                options.RejectionStatusCode =
                    StatusCodes.Status429TooManyRequests;

                options.OnRejected =
                    WriteRateLimitProblemDetailsAsync;

                AddFixedWindowPolicy(
                    options,
                    AuthenticationRateLimitPolicies.Login,
                    settings.LoginPermitLimit,
                    settings.WindowSeconds);

                AddFixedWindowPolicy(
                    options,
                    AuthenticationRateLimitPolicies.Register,
                    settings.RegisterPermitLimit,
                    settings.WindowSeconds);
            });

        return services;
    }

    private static void AddFixedWindowPolicy(
        RateLimiterOptions options,
        string policyName,
        int permitLimit,
        int windowSeconds)
    {
        options.AddPolicy(
            policyName,
            httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey:
                        GetClientPartitionKey(
                            httpContext),
                    factory:
                        _ =>
                            new FixedWindowRateLimiterOptions
                            {
                                PermitLimit = permitLimit,
                                Window =
                                    TimeSpan.FromSeconds(
                                        windowSeconds),
                                QueueLimit = 0,
                                QueueProcessingOrder =
                                    QueueProcessingOrder.OldestFirst,
                                AutoReplenishment = true
                            }));
    }

    private static string GetClientPartitionKey(
        HttpContext context)
    {
        return context.Connection.RemoteIpAddress?
                   .ToString()
               ?? "unknown-client";
    }

    private static async ValueTask
        WriteRateLimitProblemDetailsAsync(
            OnRejectedContext context,
            CancellationToken cancellationToken)
    {
        if (context.Lease.TryGetMetadata(
                MetadataName.RetryAfter,
                out var retryAfter))
        {
            var retryAfterSeconds =
                Math.Max(
                    1,
                    (int)Math.Ceiling(
                        retryAfter.TotalSeconds));

            context.HttpContext.Response.Headers.RetryAfter =
                retryAfterSeconds.ToString(
                    CultureInfo.InvariantCulture);
        }

        var problemDetailsService =
            context.HttpContext.RequestServices
                .GetRequiredService<IProblemDetailsService>();

        context.HttpContext.Response.StatusCode =
            StatusCodes.Status429TooManyRequests;

        await problemDetailsService.WriteAsync(
            new ProblemDetailsContext
            {
                HttpContext =
                    context.HttpContext,
                ProblemDetails =
                    new ProblemDetails
                    {
                        Status =
                            StatusCodes
                                .Status429TooManyRequests,
                        Title =
                            "Too Many Requests",
                        Detail =
                            "Too many authentication requests. Try again later.",
                        Instance =
                            context.HttpContext.Request.Path
                    }
            });
    }

    private static void Validate(
        AuthenticationRateLimitOptions options)
    {
        if (options.LoginPermitLimit is < 1 or > 10_000)
        {
            throw new InvalidOperationException(
                "Login rate-limit permit limit must be between 1 and 10000.");
        }

        if (options.RegisterPermitLimit is < 1 or > 10_000)
        {
            throw new InvalidOperationException(
                "Register rate-limit permit limit must be between 1 and 10000.");
        }

        if (options.WindowSeconds is < 1 or > 3600)
        {
            throw new InvalidOperationException(
                "Authentication rate-limit window must be between 1 and 3600 seconds.");
        }
    }
}
