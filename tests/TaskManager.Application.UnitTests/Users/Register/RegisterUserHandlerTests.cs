using NSubstitute;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Abstractions.Security;
using TaskManager.Application.Abstractions.Time;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Users.Register;
using TaskManager.Domain.Entities;
using Xunit;

namespace TaskManager.Application.UnitTests.Users.Register;

public sealed class RegisterUserHandlerTests
{
    [Fact]
    public async Task HandleAsyncWithValidCommandCreatesAndPersistsUser()
    {
        var userRepository =
            Substitute.For<IUserRepository>();

        var unitOfWork =
            Substitute.For<IUnitOfWork>();

        var passwordHasher =
            Substitute.For<IPasswordHasher>();

        var clock =
            Substitute.For<IClock>();

        var now = new DateTimeOffset(
            2026,
            8,
            30,
            1,
            0,
            0,
            TimeSpan.Zero);

        const string password = "StrongPassword123!";
        const string passwordHash = "hashed-password";

        clock.UtcNow.Returns(now);

        passwordHasher
            .Hash(password)
            .Returns(passwordHash);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        userRepository
            .ExistsByNormalizedEmailAsync(
                "SERGIY@EXAMPLE.COM",
                cancellationToken)
            .Returns(false);

        unitOfWork
            .SaveChangesAsync(cancellationToken)
            .Returns(1);

        var handler = new RegisterUserHandler(
            userRepository,
            unitOfWork,
            passwordHasher,
            clock);

        var command = new RegisterUserCommand(
            Email: "sergiy@example.com",
            DisplayName: "Sergiy",
            Password: password);

        var result = await handler.HandleAsync(
            command,
            cancellationToken);

        Assert.Equal(command.Email, result.Email);
        Assert.Equal(command.DisplayName, result.DisplayName);
        Assert.Equal(now, result.CreatedAtUtc);

        await userRepository
            .Received(1)
            .ExistsByNormalizedEmailAsync(
                "SERGIY@EXAMPLE.COM",
                cancellationToken);

        passwordHasher
            .Received(1)
            .Hash(password);

        userRepository
            .Received(1)
            .Add(
                Arg.Is<User>(
                    user =>
                        user.Id == result.UserId &&
                        user.Email == command.Email &&
                        user.NormalizedEmail == "SERGIY@EXAMPLE.COM" &&
                        user.DisplayName == command.DisplayName &&
                        user.PasswordHash == passwordHash &&
                        user.IsActive &&
                        user.CreatedAtUtc == now));

        await unitOfWork
            .Received(1)
            .SaveChangesAsync(cancellationToken);
    }

    [Fact]
    public async Task HandleAsyncWhenEmailAlreadyExistsThrowsConflictException()
    {
        var userRepository =
            Substitute.For<IUserRepository>();

        var unitOfWork =
            Substitute.For<IUnitOfWork>();

        var passwordHasher =
            Substitute.For<IPasswordHasher>();

        var clock =
            Substitute.For<IClock>();

        var cancellationToken =
            TestContext.Current.CancellationToken;

        userRepository
            .ExistsByNormalizedEmailAsync(
                "SERGIY@EXAMPLE.COM",
                cancellationToken)
            .Returns(true);

        var handler = new RegisterUserHandler(
            userRepository,
            unitOfWork,
            passwordHasher,
            clock);

        var command = new RegisterUserCommand(
            Email: "sergiy@example.com",
            DisplayName: "Sergiy",
            Password: "StrongPassword123!");

        var exception =
            await Assert.ThrowsAsync<ApplicationConflictException>(
                () => handler.HandleAsync(
                    command,
                    cancellationToken));

        Assert.Equal(
            "A user with this email already exists.",
            exception.Message);

        await userRepository
            .Received(1)
            .ExistsByNormalizedEmailAsync(
                "SERGIY@EXAMPLE.COM",
                cancellationToken);

        passwordHasher
            .DidNotReceive()
            .Hash(Arg.Any<string>());

        userRepository
            .DidNotReceive()
            .Add(Arg.Any<User>());

        await unitOfWork
            .DidNotReceive()
            .SaveChangesAsync(
                Arg.Any<CancellationToken>());
    }
}