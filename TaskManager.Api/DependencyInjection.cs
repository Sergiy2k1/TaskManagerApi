using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using TaskManager.Api.Authentication;
using TaskManager.Api.ErrorHandling;
using TaskManager.Api.Health;
using TaskManager.Api.Observability;
using TaskManager.Api.RateLimiting;
using TaskManager.Application.Abstractions.Authentication;
using TaskManager.Infrastructure.Security;

namespace TaskManager.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddPresentation(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddControllers();
        services.AddOpenApi();
        services.AddProblemDetails(
            options =>
            {
                options.CustomizeProblemDetails =
                    context =>
                    {
                        context.ProblemDetails
                            .Extensions["traceId"] =
                            RequestCorrelation.GetTraceId(
                                context.HttpContext);

                        context.ProblemDetails
                            .Extensions["correlationId"] =
                            RequestCorrelation.GetCorrelationId(
                                context.HttpContext);
                    };
            });
        services.AddExceptionHandler<GlobalExceptionHandler>();

        services
            .AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>(
                "postgresql",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["ready"]);

        services.AddTaskManagerOpenTelemetry(
            configuration);

        services.AddTaskManagerRateLimiting(
            configuration);

        AddAuthentication(services, configuration);

        services.AddAuthorization();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();

        return services;
    }

    private static void AddAuthentication(
        IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection(JwtOptions.SectionName);

        var jwtOptions = jwtSection.Get<JwtOptions>()
            ?? throw new InvalidOperationException(
                "JWT configuration was not found.");

        services
            .AddOptions<JwtOptions>()
            .Bind(jwtSection)
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Issuer),
                "Jwt:Issuer is required.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Audience),
                "Jwt:Audience is required.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.SigningKey),
                "Jwt:SigningKey is required.")
            .Validate(
                options => options.AccessTokenLifetimeMinutes is >= 1 and <= 60,
                "Jwt:AccessTokenLifetimeMinutes must be between 1 and 60.")
            .ValidateOnStart();

        byte[] signingKeyBytes;

        try
        {
            signingKeyBytes = Convert.FromBase64String(jwtOptions.SigningKey);
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException(
                "Jwt:SigningKey must be a valid Base64 string.",
                exception);
        }

        if (signingKeyBytes.Length < 32)
        {
            throw new InvalidOperationException(
                "Jwt:SigningKey must contain at least 32 bytes.");
        }

        var securityKey = new SymmetricSecurityKey(signingKeyBytes);

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.Events = new JwtBearerEvents
                {
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();

                        context.Response.StatusCode =
                            StatusCodes.Status401Unauthorized;

                        context.Response.Headers.WWWAuthenticate =
                            JwtBearerDefaults.AuthenticationScheme;

                        var problemDetailsService =
                            context.HttpContext.RequestServices
                                .GetRequiredService<IProblemDetailsService>();

                        await problemDetailsService.WriteAsync(
                            new ProblemDetailsContext
                            {
                                HttpContext = context.HttpContext,
                                ProblemDetails = new ProblemDetails
                                {
                                    Status =
                                        StatusCodes.Status401Unauthorized,
                                    Title = "Unauthorized",
                                    Detail =
                                        "Authentication is required.",
                                    Instance =
                                        context.HttpContext.Request.Path
                                }
                            });
                    },
                    OnForbidden = async context =>
                    {
                        context.Response.StatusCode =
                            StatusCodes.Status403Forbidden;

                        var problemDetailsService =
                            context.HttpContext.RequestServices
                                .GetRequiredService<IProblemDetailsService>();

                        await problemDetailsService.WriteAsync(
                            new ProblemDetailsContext
                            {
                                HttpContext = context.HttpContext,
                                ProblemDetails = new ProblemDetails
                                {
                                    Status =
                                        StatusCodes.Status403Forbidden,
                                    Title = "Forbidden",
                                    Detail =
                                        "You do not have permission to access this resource.",
                                    Instance =
                                        context.HttpContext.Request.Path
                                }
                            });
                    }
                };
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = securityKey,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });
    }
}
