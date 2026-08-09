namespace TaskManager.Domain.Entities;

public sealed class Project
{
    public const int MinNameLength = 2;
    public const int MaxNameLength = 150;
    public const int MaxDescriptionLength = 2000;

    public Guid Id { get; private set; }

    public Guid OwnerId { get; private set; }

    public string Name { get; private set; } = null!;

    public string? Description { get; private set; }

    public bool IsArchived { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public DateTimeOffset? ArchivedAtUtc { get; private set; }

    private Project()
    {
    }

    public static Project Create(
        Guid ownerId,
        string name,
        string? description,
        DateTimeOffset createdAtUtc)
    {
        ValidateOwnerId(ownerId);

        var preparedName = PrepareName(name);
        var preparedDescription = PrepareDescription(description);

        EnsureUtc(createdAtUtc, nameof(createdAtUtc));

        return new Project
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Name = preparedName,
            Description = preparedDescription,
            IsArchived = false,
            CreatedAtUtc = createdAtUtc
        };
    }

    public void Rename(
        string name,
        DateTimeOffset changedAtUtc)
    {
        EnsureCanBeModified();

        var preparedName = PrepareName(name);

        EnsureValidChangeTime(
            changedAtUtc,
            nameof(changedAtUtc));

        if (string.Equals(
                Name,
                preparedName,
                StringComparison.Ordinal))
        {
            return;
        }

        Name = preparedName;
        UpdatedAtUtc = changedAtUtc;
    }

    public void ChangeDescription(
        string? description,
        DateTimeOffset changedAtUtc)
    {
        EnsureCanBeModified();

        var preparedDescription =
            PrepareDescription(description);

        EnsureValidChangeTime(
            changedAtUtc,
            nameof(changedAtUtc));

        if (string.Equals(
                Description,
                preparedDescription,
                StringComparison.Ordinal))
        {
            return;
        }

        Description = preparedDescription;
        UpdatedAtUtc = changedAtUtc;
    }

    public void Archive(DateTimeOffset archivedAtUtc)
    {
        EnsureValidChangeTime(
            archivedAtUtc,
            nameof(archivedAtUtc));

        if (IsArchived)
        {
            return;
        }

        IsArchived = true;
        ArchivedAtUtc = archivedAtUtc;
        UpdatedAtUtc = archivedAtUtc;
    }

    public void Restore(DateTimeOffset restoredAtUtc)
    {
        EnsureValidChangeTime(
            restoredAtUtc,
            nameof(restoredAtUtc));

        if (!IsArchived)
        {
            return;
        }

        IsArchived = false;
        ArchivedAtUtc = null;
        UpdatedAtUtc = restoredAtUtc;
    }

    private void EnsureCanBeModified()
    {
        if (IsArchived)
        {
            throw new InvalidOperationException(
                "Archived project cannot be modified.");
        }
    }

    private static void ValidateOwnerId(Guid ownerId)
    {
        if (ownerId == Guid.Empty)
        {
            throw new ArgumentException(
                "Project owner identifier cannot be empty.",
                nameof(ownerId));
        }
    }

    private static string PrepareName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Project name cannot be empty.",
                nameof(name));
        }

        var preparedName = name.Trim();

        if (preparedName.Length < MinNameLength)
        {
            throw new ArgumentException(
                $"Project name must contain at least {MinNameLength} characters.",
                nameof(name));
        }

        if (preparedName.Length > MaxNameLength)
        {
            throw new ArgumentException(
                $"Project name cannot exceed {MaxNameLength} characters.",
                nameof(name));
        }

        return preparedName;
    }

    private static string? PrepareDescription(
        string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var preparedDescription = description.Trim();

        if (preparedDescription.Length > MaxDescriptionLength)
        {
            throw new ArgumentException(
                $"Project description cannot exceed {MaxDescriptionLength} characters.",
                nameof(description));
        }

        return preparedDescription;
    }

    private void EnsureValidChangeTime(
        DateTimeOffset changedAtUtc,
        string parameterName)
    {
        EnsureUtc(changedAtUtc, parameterName);

        if (changedAtUtc < CreatedAtUtc)
        {
            throw new ArgumentException(
                "Change time cannot be earlier than project creation time.",
                parameterName);
        }

        if (UpdatedAtUtc.HasValue &&
            changedAtUtc < UpdatedAtUtc.Value)
        {
            throw new ArgumentException(
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
            throw new ArgumentException(
                "Date and time must be in UTC.",
                parameterName);
        }
    }
}