using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using TaskManager.Api.Authentication;
using TaskManager.Api.ErrorHandling;
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
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

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
