using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManager.Domain.Entities;

namespace TaskManager.Infrastructure.Persistence.Configurations;

public sealed class ProjectConfiguration
    : IEntityTypeConfiguration<Project>
{
    public void Configure(
        EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("projects");

        builder.HasKey(project => project.Id)
            .HasName("pk_projects");

        builder.Property(project => project.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(project => project.OwnerId)
            .HasColumnName("owner_id")
            .IsRequired();

        builder.Property(project => project.Name)
            .HasColumnName("name")
            .HasMaxLength(Project.MaxNameLength)
            .IsRequired();

        builder.Property(project => project.Description)
            .HasColumnName("description")
            .HasMaxLength(Project.MaxDescriptionLength);

        builder.Property(project => project.IsArchived)
            .HasColumnName("is_archived")
            .IsRequired();

        builder.Property(project => project.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(project => project.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.Property(project => project.ArchivedAtUtc)
            .HasColumnName("archived_at_utc");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(project => project.OwnerId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_projects_users_owner_id");

        builder.HasIndex(project => project.OwnerId)
            .HasDatabaseName(
                "ix_projects_owner_id");
    }
}