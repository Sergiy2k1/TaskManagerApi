namespace TaskManager.Api.Contracts.Auth;

public sealed record LoginResponse(
    Guid UserId,
    string Email,
    string DisplayName,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc);