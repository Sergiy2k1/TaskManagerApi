namespace TaskManager.Api.RateLimiting;

public sealed class AuthenticationRateLimitOptions
{
    public const string SectionName =
        "RateLimiting:Authentication";

    public int LoginPermitLimit { get; init; } = 10;

    public int RegisterPermitLimit { get; init; } = 5;

    public int WindowSeconds { get; init; } = 60;
}
