using TaskManager.Domain.Enums;
using TaskManager.Domain.Exceptions;

namespace TaskManager.Domain.Entities;

public sealed class ProjectMember
{
    public Guid Id { get; private set; }

    public Guid ProjectId { get; private set; }

    public Guid UserId { get; private set; }

    public ProjectMemberRole Role { get; private set; }

    public DateTimeOffset JoinedAtUtc { get; private set; }

    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public DateTimeOffset? RemovedAtUtc { get; private set; }

    public bool IsActive => RemovedAtUtc is null;

    private ProjectMember()
    {
    }

    public static ProjectMember Create(
        Guid projectId,
        Guid userId,
        ProjectMemberRole role,
        DateTimeOffset joinedAtUtc)
    {
        ValidateIdentifier(
            projectId,
            nameof(projectId),
            "Project identifier cannot be empty.");

        ValidateIdentifier(
            userId,
            nameof(userId),
            "User identifier cannot be empty.");

        ValidateRole(role);

        EnsureUtc(
            joinedAtUtc,
            nameof(joinedAtUtc));

        return new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            UserId = userId,
            Role = role,
            JoinedAtUtc = joinedAtUtc
        };
    }

    public void ChangeRole(
        ProjectMemberRole role,
        DateTimeOffset changedAtUtc)
    {
        EnsureActive();

        ValidateRole(role);

        EnsureValidChangeTime(
            changedAtUtc,
            nameof(changedAtUtc));

        if (Role == role)
        {
            return;
        }

        Role = role;
        UpdatedAtUtc = changedAtUtc;
    }

    public void Remove(DateTimeOffset removedAtUtc)
    {
        if (!IsActive)
        {
            return;
        }

        EnsureValidChangeTime(
            removedAtUtc,
            nameof(removedAtUtc));

        RemovedAtUtc = removedAtUtc;
        UpdatedAtUtc = removedAtUtc;
    }

    public void Restore(DateTimeOffset restoredAtUtc)
    {
        if (IsActive)
        {
            return;
        }

        EnsureValidChangeTime(
            restoredAtUtc,
            nameof(restoredAtUtc));

        RemovedAtUtc = null;
        UpdatedAtUtc = restoredAtUtc;
    }

    private void EnsureActive()
    {
        if (!IsActive)
        {
            throw new DomainConflictException(
                "Removed project member cannot be modified.");
        }
    }

    private static void ValidateRole(ProjectMemberRole role)
    {
        if (!Enum.IsDefined(role))
        {
            throw new DomainValidationException(
                $"Unsupported project member role: {role}.",
                nameof(role));
        }
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

        if (changedAtUtc < JoinedAtUtc)
        {
            throw new DomainValidationException(
                "Change time cannot be earlier than the member joining time.",
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