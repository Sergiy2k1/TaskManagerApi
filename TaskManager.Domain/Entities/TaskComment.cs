using TaskManager.Domain.Exceptions;

namespace TaskManager.Domain.Entities;

public sealed class TaskComment
{
    public const int MaxContentLength = 2000;

    public Guid Id { get; private set; }

    public Guid TaskItemId { get; private set; }

    public Guid AuthorUserId { get; private set; }

    public string Content { get; private set; } = null!;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public DateTimeOffset? DeletedAtUtc { get; private set; }

    public bool IsDeleted => DeletedAtUtc is not null;

    private TaskComment()
    {
    }

    public static TaskComment Create(
        Guid taskItemId,
        Guid authorUserId,
        string content,
        DateTimeOffset createdAtUtc)
    {
        ValidateIdentifier(
            taskItemId,
            nameof(taskItemId),
            "Task identifier cannot be empty.");

        ValidateIdentifier(
            authorUserId,
            nameof(authorUserId),
            "Author identifier cannot be empty.");

        var preparedContent = PrepareContent(content);

        EnsureUtc(
            createdAtUtc,
            nameof(createdAtUtc));

        return new TaskComment
        {
            Id = Guid.NewGuid(),
            TaskItemId = taskItemId,
            AuthorUserId = authorUserId,
            Content = preparedContent,
            CreatedAtUtc = createdAtUtc
        };
    }

    public void Edit(
        string content,
        DateTimeOffset editedAtUtc)
    {
        EnsureNotDeleted();

        var preparedContent = PrepareContent(content);

        EnsureValidChangeTime(
            editedAtUtc,
            nameof(editedAtUtc));

        if (string.Equals(
                Content,
                preparedContent,
                StringComparison.Ordinal))
        {
            return;
        }

        Content = preparedContent;
        UpdatedAtUtc = editedAtUtc;
    }

    public void Delete(DateTimeOffset deletedAtUtc)
    {
        if (IsDeleted)
        {
            return;
        }

        EnsureValidChangeTime(
            deletedAtUtc,
            nameof(deletedAtUtc));

        DeletedAtUtc = deletedAtUtc;
        UpdatedAtUtc = deletedAtUtc;
    }

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
        {
            throw new DomainConflictException(
                "Deleted comment cannot be modified.");
        }
    }

    private static string PrepareContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new DomainValidationException(
                "Comment content cannot be empty.",
                nameof(content));
        }

        var preparedContent = content.Trim();

        if (preparedContent.Length > MaxContentLength)
        {
            throw new DomainValidationException(
                $"Comment content cannot exceed {MaxContentLength} characters.",
                nameof(content));
        }

        return preparedContent;
    }

    private static void ValidateIdentifier(
        Guid identifier,
        string parameterName,
        string errorMessage)
    {
        if (identifier == Guid.Empty)
        {
            throw new DomainValidationException(
                errorMessage,
                parameterName);
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
                "Change time cannot be earlier than comment creation time.",
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