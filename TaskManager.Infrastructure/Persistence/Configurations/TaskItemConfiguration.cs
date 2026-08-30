using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManager.Domain.Entities;

namespace TaskManager.Infrastructure.Persistence.Configurations;

public sealed class TaskItemConfiguration
    : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(
        EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("task_items");

        builder.HasKey(task => task.Id)
            .HasName("pk_task_items");

        builder.Property(task => task.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(task => task.ProjectId)
            .HasColumnName("project_id")
            .IsRequired();

        builder.Property(task => task.CreatedByUserId)
            .HasColumnName("created_by_user_id")
            .IsRequired();

        builder.Property(task => task.AssigneeId)
            .HasColumnName("assignee_id");

        builder.Property(task => task.Title)
            .HasColumnName("title")
            .HasMaxLength(TaskItem.MaxTitleLength)
            .IsRequired();

        builder.Property(task => task.Description)
            .HasColumnName("description")
            .HasMaxLength(TaskItem.MaxDescriptionLength);

        builder.Property(task => task.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(task => task.Priority)
            .HasColumnName("priority")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(task => task.DueDateUtc)
            .HasColumnName("due_date_utc");

        builder.Property(task => task.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(task => task.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.Property(task => task.CompletedAtUtc)
            .HasColumnName("completed_at_utc");

        builder.HasOne<Project>()
            .WithMany()
            .HasForeignKey(task => task.ProjectId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_task_items_projects_project_id");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(task => task.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_task_items_users_created_by_user_id");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(task => task.AssigneeId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_task_items_users_assignee_id");

        builder.HasIndex(task => task.ProjectId)
            .HasDatabaseName(
                "ix_task_items_project_id");

        builder.HasIndex(task => task.AssigneeId)
            .HasDatabaseName(
                "ix_task_items_assignee_id");
    }
}