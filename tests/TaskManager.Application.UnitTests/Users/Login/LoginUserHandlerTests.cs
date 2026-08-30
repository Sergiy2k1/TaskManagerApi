using NSubstitute;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Abstractions.Security;
using TaskManager.Application.Abstractions.Time;
using TaskManager.Application.Users.Login;
using TaskManager.Domain.Entities;
using Xunit;

namespace TaskManager.Application.UnitTests.Users.Login;

public sealed class LoginUserHandlerTests
{
    [Fact]
    public async Task HandleAsyncWithValidCredentialsReturnsAccessToken()
    {
        var userRepository =
            Substitute.For<IUserRepository>();

        var passwordHasher =
            Substitute.For<IPasswordHasher>();

        var accessTokenGenerator =
            Substitute.For<IAccessTokenGenerator>();

        var clock =
            Substitute.For<IClock>();

        var now = new DateTimeOffset(
            2026,
            8,
            30,
            18,
            0,
            0,
            TimeSpan.Zero);

        var expiresAtUtc =
            now.AddHours(1);

        const string password = "StrongPassword123!";
        const string passwordHash = "hashed-password";
        const string accessTokenValue = "access-token";

        var user = User.Create(
            email: "sergiy@example.com",
            displayName: "Sergiy",
            passwordHash: passwordHash,
            createdAtUtc: now.AddDays(-1));

        var cancellationToken =
            TestContext.Current.CancellationToken;

        userRepository
            .GetByNormalizedEmailAsync(
                "SERGIY@EXAMPLE.COM",
                cancellationToken)
            .Returns(user);

        passwordHasher
            .Verify(
                password,
                passwordHash)
            .Returns(true);

        clock.UtcNow.Returns(now);

        accessTokenGenerator
            .Generate(
                user,
                now)
            .Returns(
                new AccessToken(
                    Value: accessTokenValue,
                    ExpiresAtUtc: expiresAtUtc));

        var handler = new LoginUserHandler(
            userRepository,
            passwordHasher,
            accessTokenGenerator,
            clock);

        var command = new LoginUserCommand(
            Email: "sergiy@example.com",
            Password: password);

        var result = await handler.HandleAsync(
            command,
            cancellationToken);

        Assert.Equal(user.Id, result.UserId);
        Assert.Equal(user.Email, result.Email);
        Assert.Equal(user.DisplayName, result.DisplayName);
        Assert.Equal(accessTokenValue, result.AccessToken);
        Assert.Equal(
            expiresAtUtc,
            result.AccessTokenExpiresAtUtc);

        await userRepository
            .Received(1)
            .GetByNormalizedEmailAsync(
                "SERGIY@EXAMPLE.COM",
                cancellationToken);

        passwordHasher
            .Received(1)
            .Verify(
                password,
                passwordHash);

        accessTokenGenerator
            .Received(1)
            .Generate(
                user,
                now);
    }
}