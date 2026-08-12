// LoginUserResult.cs
namespace TaskManager.Application.Users.Login;

public sealed record LoginUserResult(
    Guid UserId,
    string Email,
    string DisplayName,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc);