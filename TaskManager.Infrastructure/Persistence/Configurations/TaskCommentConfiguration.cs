using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManager.Domain.Entities;

namespace TaskManager.Infrastructure.Persistence.Configurations;

public sealed class TaskCommentConfiguration
    : IEntityTypeConfiguration<TaskComment>
{
    public void Configure(
        EntityTypeBuilder<TaskComment> builder)
    {
        builder.ToTable("task_comments");

        builder.HasKey(comment => comment.Id)
            .HasName("pk_task_comments");

        builder.Property(comment => comment.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(comment => comment.TaskItemId)
            .HasColumnName("task_item_id")
            .IsRequired();

        builder.Property(comment => comment.AuthorUserId)
            .HasColumnName("author_user_id")
            .IsRequired();

        builder.Property(comment => comment.Content)
            .HasColumnName("content")
            .HasMaxLength(TaskComment.MaxContentLength)
            .IsRequired();

        builder.Property(comment => comment.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(comment => comment.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.Property(comment => comment.DeletedAtUtc)
            .HasColumnName("deleted_at_utc");

        builder.Ignore(comment => comment.IsDeleted);

        builder.HasOne<TaskItem>()
            .WithMany()
            .HasForeignKey(comment => comment.TaskItemId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_task_comments_task_items_task_item_id");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(comment => comment.AuthorUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_task_comments_users_author_user_id");

        builder.HasIndex(comment => comment.TaskItemId)
            .HasDatabaseName(
                "ix_task_comments_task_item_id");

        builder.HasIndex(comment => comment.AuthorUserId)
            .HasDatabaseName(
                "ix_task_comments_author_user_id");
    }
}