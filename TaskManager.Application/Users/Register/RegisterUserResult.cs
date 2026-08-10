namespace TaskManager.Application.Users.Register;

public sealed record RegisterUserResult(
    Guid UserId,
    string Email,
    string DisplayName,
    DateTimeOffset CreatedAtUtc);