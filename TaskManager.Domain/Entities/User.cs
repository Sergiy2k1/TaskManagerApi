using System.Net.Mail;
using TaskManager.Domain.Exceptions;

namespace TaskManager.Domain.Entities;

public sealed class User
{
    public const int MinDisplayNameLength = 2;
    public const int MaxDisplayNameLength = 100;
    public const int MaxEmailLength = 320;

    public Guid Id { get; private set; }

    public string Email { get; private set; } = null!;

    public string NormalizedEmail { get; private set; } = null!;

    public string DisplayName { get; private set; } = null!;

    public string PasswordHash { get; private set; } = null!;

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    private User()
    {
    }

    public static User Create(
        string email,
        string displayName,
        string passwordHash,
        DateTimeOffset createdAtUtc)
    {
        var preparedEmail = PrepareEmail(email);
        var preparedDisplayName = PrepareDisplayName(displayName);

        ValidatePasswordHash(passwordHash);

        EnsureUtc(
            createdAtUtc,
            nameof(createdAtUtc));

        return new User
        {
            Id = Guid.NewGuid(),
            Email = preparedEmail,
            NormalizedEmail = preparedEmail.ToUpperInvariant(),
            DisplayName = preparedDisplayName,
            PasswordHash = passwordHash,
            IsActive = true,
            CreatedAtUtc = createdAtUtc
        };
    }

    public void ChangeDisplayName(
        string displayName,
        DateTimeOffset changedAtUtc)
    {
        var preparedDisplayName =
            PrepareDisplayName(displayName);

        EnsureValidChangeTime(
            changedAtUtc,
            nameof(changedAtUtc));

        if (string.Equals(
                DisplayName,
                preparedDisplayName,
                StringComparison.Ordinal))
        {
            return;
        }

        DisplayName = preparedDisplayName;
        UpdatedAtUtc = changedAtUtc;
    }

    public void ChangePasswordHash(
        string passwordHash,
        DateTimeOffset changedAtUtc)
    {
        ValidatePasswordHash(passwordHash);

        EnsureValidChangeTime(
            changedAtUtc,
            nameof(changedAtUtc));

        if (string.Equals(
                PasswordHash,
                passwordHash,
                StringComparison.Ordinal))
        {
            return;
        }

        PasswordHash = passwordHash;
        UpdatedAtUtc = changedAtUtc;
    }

    public void Activate(DateTimeOffset changedAtUtc)
    {
        EnsureValidChangeTime(
            changedAtUtc,
            nameof(changedAtUtc));

        if (IsActive)
        {
            return;
        }

        IsActive = true;
        UpdatedAtUtc = changedAtUtc;
    }

    public void Deactivate(DateTimeOffset changedAtUtc)
    {
        EnsureValidChangeTime(
            changedAtUtc,
            nameof(changedAtUtc));

        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        UpdatedAtUtc = changedAtUtc;
    }

    private static string PrepareEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainValidationException(
                "Email cannot be empty.",
                nameof(email));
        }

        var preparedEmail = email.Trim();

        if (preparedEmail.Length > MaxEmailLength)
        {
            throw new DomainValidationException(
                $"Email cannot exceed {MaxEmailLength} characters.",
                nameof(email));
        }

        var isValidEmail =
            MailAddress.TryCreate(
                preparedEmail,
                out var parsedEmail)
            &&
            string.Equals(
                parsedEmail.Address,
                preparedEmail,
                StringComparison.OrdinalIgnoreCase);

        if (!isValidEmail)
        {
            throw new DomainValidationException(
                "Email has an invalid format.",
                nameof(email));
        }

        return preparedEmail;
    }

    private static string PrepareDisplayName(
        string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new DomainValidationException(
                "Display name cannot be empty.",
                nameof(displayName));
        }

        var preparedDisplayName = displayName.Trim();

        if (preparedDisplayName.Length < MinDisplayNameLength)
        {
            throw new DomainValidationException(
                $"Display name must contain at least {MinDisplayNameLength} characters.",
                nameof(displayName));
        }

        if (preparedDisplayName.Length > MaxDisplayNameLength)
        {
            throw new DomainValidationException(
                $"Display name cannot exceed {MaxDisplayNameLength} characters.",
                nameof(displayName));
        }

        return preparedDisplayName;
    }

    private static void ValidatePasswordHash(
        string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainValidationException(
                "Password hash cannot be empty.",
                nameof(passwordHash));
        }
    }

    private void EnsureValidChangeTime(
        DateTimeOffset changedAtUtc,
        string parameterName)
    {
        EnsureUtc(
            changedAtUtc,
            parameterName);

        if (changedAtUtc < CreatedAtUtc)
        {
            throw new DomainValidationException(
                "Change time cannot be earlier than user creation time.",
                parameterName);
        }

        if (UpdatedAtUtc.HasValue &&
            changedAtUtc < UpdatedAtUtc.Value)
        {
            throw new DomainValidationException(
                "Change time cannot be earlier than the previous change time.",
                parameterName);
        }
    }

    private static void EnsureUtc(
        DateTimeOffset value,
        string parameterName)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainValidationException(
                "Date and time must be in UTC.",
                parameterName);
        }
    }
}