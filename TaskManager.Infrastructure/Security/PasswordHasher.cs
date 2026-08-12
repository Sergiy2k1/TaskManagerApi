using Microsoft.AspNetCore.Identity;
using TaskManager.Application.Abstractions.Security;

namespace TaskManager.Infrastructure.Security;

public sealed class PasswordHasher : IPasswordHasher
{
    private static readonly object UserMarker = new();

    private readonly PasswordHasher<object> _passwordHasher = new();

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        return _passwordHasher.HashPassword(
            UserMarker,
            password);
    }

    public bool Verify(
        string password,
        string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        var result = _passwordHasher.VerifyHashedPassword(
            UserMarker,
            passwordHash,
            password);

        return result is
            PasswordVerificationResult.Success
            or PasswordVerificationResult.SuccessRehashNeeded;
    }
}