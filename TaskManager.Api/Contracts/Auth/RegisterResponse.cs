namespace TaskManager.Api.Contracts.Auth;

public sealed record RegisterResponse(
    Guid UserId,
    string Email,
    string DisplayName,
    DateTimeOffset CreatedAtUtc);