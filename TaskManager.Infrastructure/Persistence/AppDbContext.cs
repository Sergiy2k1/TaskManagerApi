using Microsoft.EntityFrameworkCore;
using Npgsql;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Domain.Entities;

namespace TaskManager.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext, IUnitOfWork
{
    private const string ConcurrencyConflictMessage =
        "The resource was modified by another request. Reload the resource and retry.";

    private const string UserEmailConstraint =
        "ux_users_normalized_email";

    private const string ProjectMemberConstraint =
        "ux_project_members_project_id_user_id";

    public AppDbContext(
        DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<Project> Projects => Set<Project>();

    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();

    public DbSet<TaskItem> TaskItems => Set<TaskItem>();

    public DbSet<TaskComment> TaskComments => Set<TaskComment>();

    public override int SaveChanges()
    {
        PrepareConcurrencyTokens();

        try
        {
            return base.SaveChanges();
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ApplicationConflictException(
                ConcurrencyConflictMessage,
                exception);
        }
        catch (DbUpdateException exception)
        {
            var conflict =
                MapDatabaseConflict(exception);

            if (conflict is not null)
            {
                throw conflict;
            }

            throw;
        }
    }

    public override async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        PrepareConcurrencyTokens();

        try
        {
            return await base.SaveChangesAsync(
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ApplicationConflictException(
                ConcurrencyConflictMessage,
                exception);
        }
        catch (DbUpdateException exception)
        {
            var conflict =
                MapDatabaseConflict(exception);

            if (conflict is not null)
            {
                throw conflict;
            }

            throw;
        }
    }

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(AppDbContext).Assembly);
    }

    private static ApplicationConflictException?
        MapDatabaseConflict(
            DbUpdateException exception)
    {
        if (exception.InnerException
                is not PostgresException postgresException ||
            postgresException.SqlState !=
                PostgresErrorCodes.UniqueViolation)
        {
            return null;
        }

        var message =
            postgresException.ConstraintName switch
            {
                UserEmailConstraint =>
                    "A user with this email already exists.",

                ProjectMemberConstraint =>
                    "User is already an active project member.",

                _ => null
            };

        return message is null
            ? null
            : new ApplicationConflictException(
                message,
                exception);
    }

    private void PrepareConcurrencyTokens()
    {
        ChangeTracker.DetectChanges();

        foreach (var entry in ChangeTracker.Entries<Project>())
        {
            if (entry.State != EntityState.Modified)
            {
                continue;
            }

            var version =
                entry.Property(project => project.Version);

            version.CurrentValue =
                checked(version.OriginalValue + 1);
        }

        foreach (var entry in ChangeTracker.Entries<TaskItem>())
        {
            if (entry.State != EntityState.Modified)
            {
                continue;
            }

            var version =
                entry.Property(taskItem => taskItem.Version);

            version.CurrentValue =
                checked(version.OriginalValue + 1);
        }
    }
}
