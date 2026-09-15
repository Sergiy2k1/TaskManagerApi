using Microsoft.EntityFrameworkCore;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Domain.Entities;

namespace TaskManager.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext, IUnitOfWork
{
    private const string ConcurrencyConflictMessage =
        "The resource was modified by another request. Reload the resource and retry.";

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
    }

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(AppDbContext).Assembly);
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
