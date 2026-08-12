// LoginUserHandler.cs
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Abstractions.Security;
using TaskManager.Application.Abstractions.Time;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Domain.Entities;

namespace TaskManager.Application.Users.Login;

public sealed class LoginUserHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAccessTokenGenerator _accessTokenGenerator;
    private readonly IClock _clock;

    public LoginUserHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IAccessTokenGenerator accessTokenGenerator,
        IClock clock)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _accessTokenGenerator = accessTokenGenerator;
        _clock = clock;
    }

    public async Task<LoginUserResult> HandleAsync(
        LoginUserCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.Password))
        {
            throw new ApplicationUnauthorizedException(
                "Invalid email or password.");
        }

        var normalizedEmail = User.NormalizeEmail(command.Email);

        var user = await _userRepository.GetByNormalizedEmailAsync(
            normalizedEmail,
            cancellationToken);

        if (user is null || !user.IsActive)
        {
            throw new ApplicationUnauthorizedException(
                "Invalid email or password.");
        }

        var passwordIsValid = _passwordHasher.Verify(
            command.Password,
            user.PasswordHash);

        if (!passwordIsValid)
        {
            throw new ApplicationUnauthorizedException(
                "Invalid email or password.");
        }

        var now = _clock.UtcNow;

        var accessToken = _accessTokenGenerator.Generate(
            user,
            now);

        return new LoginUserResult(
            UserId: user.Id,
            Email: user.Email,
            DisplayName: user.DisplayName,
            AccessToken: accessToken.Value,
            AccessTokenExpiresAtUtc: accessToken.ExpiresAtUtc);
    }
}