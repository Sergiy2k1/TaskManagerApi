using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Abstractions.Security;
using TaskManager.Application.Abstractions.Time;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Domain.Entities;
using TaskManager.Application.Abstractions.Messaging;

namespace TaskManager.Application.Users.Register;

public sealed class RegisterUserHandler
    : ICommandHandler<RegisterUserCommand, RegisterUserResult>
{
    public const int MinPasswordLength = 8;
    public const int MaxPasswordLength = 128;

    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IClock _clock;

    public RegisterUserHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IClock clock)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _clock = clock;
    }

    public async Task<RegisterUserResult> HandleAsync(
        RegisterUserCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        ValidatePassword(command.Password);

        var normalizedEmail =
            User.NormalizeEmail(command.Email);

        var emailAlreadyExists =
            await _userRepository.ExistsByNormalizedEmailAsync(
                normalizedEmail,
                cancellationToken);

        if (emailAlreadyExists)
        {
            throw new ApplicationConflictException(
                "A user with this email already exists.");
        }

        var passwordHash =
            _passwordHasher.Hash(command.Password);

        var now = _clock.UtcNow;

        var user = User.Create(
            email: command.Email,
            displayName: command.DisplayName,
            passwordHash: passwordHash,
            createdAtUtc: now);

        _userRepository.Add(user);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return new RegisterUserResult(
            UserId: user.Id,
            Email: user.Email,
            DisplayName: user.DisplayName,
            CreatedAtUtc: user.CreatedAtUtc);
    }

    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ApplicationValidationException(
                "Password cannot be empty.",
                nameof(password));
        }

        if (password.Length < MinPasswordLength)
        {
            throw new ApplicationValidationException(
                $"Password must contain at least {MinPasswordLength} characters.",
                nameof(password));
        }

        if (password.Length > MaxPasswordLength)
        {
            throw new ApplicationValidationException(
                $"Password cannot exceed {MaxPasswordLength} characters.",
                nameof(password));
        }
    }
}