using Microsoft.IdentityModel.JsonWebTokens;
using TaskManager.Application.Abstractions.Authentication;
using TaskManager.Application.Common.Exceptions;

namespace TaskManager.Api.Authentication;

public sealed class HttpCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpCurrentUser(
        IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid UserId
    {
        get
        {
            var userIdValue =
                GetRequiredClaim(JwtRegisteredClaimNames.Sub);

            if (!Guid.TryParse(userIdValue, out var userId))
            {
                throw new ApplicationUnauthorizedException(
                    "Authenticated user identifier is invalid.");
            }

            return userId;
        }
    }

    public string? Email =>
        GetOptionalClaim(JwtRegisteredClaimNames.Email);

    public string? DisplayName =>
        GetOptionalClaim("display_name");

    private string GetRequiredClaim(string claimType)
    {
        var value = GetOptionalClaim(claimType);

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ApplicationUnauthorizedException(
                "Authenticated user information is missing.");
        }

        return value;
    }

    private string? GetOptionalClaim(string claimType)
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext is null)
        {
            throw new ApplicationUnauthorizedException(
                "HTTP context is not available.");
        }

        if (httpContext.User.Identity?.IsAuthenticated != true)
        {
            throw new ApplicationUnauthorizedException(
                "User is not authenticated.");
        }

        return httpContext.User
            .FindFirst(claimType)?
            .Value;
    }
}