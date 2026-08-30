using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManager.Domain.Entities;

namespace TaskManager.Infrastructure.Persistence.Configurations;

public sealed class ProjectMemberConfiguration
    : IEntityTypeConfiguration<ProjectMember>
{
    public void Configure(
        EntityTypeBuilder<ProjectMember> builder)
    {
        builder.ToTable("project_members");

        builder.HasKey(member => member.Id)
            .HasName("pk_project_members");

        builder.Property(member => member.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(member => member.ProjectId)
            .HasColumnName("project_id")
            .IsRequired();

        builder.Property(member => member.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(member => member.Role)
            .HasColumnName("role")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(member => member.JoinedAtUtc)
            .HasColumnName("joined_at_utc")
            .IsRequired();

        builder.Property(member => member.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.Property(member => member.RemovedAtUtc)
            .HasColumnName("removed_at_utc");

        builder.Ignore(member => member.IsActive);

        builder.HasOne<Project>()
            .WithMany()
            .HasForeignKey(member => member.ProjectId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_project_members_projects_project_id");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(member => member.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_project_members_users_user_id");

        builder.HasIndex(
                member => new
                {
                    member.ProjectId,
                    member.UserId
                })
            .IsUnique()
            .HasDatabaseName(
                "ux_project_members_project_id_user_id");

        builder.HasIndex(member => member.UserId)
            .HasDatabaseName(
                "ix_project_members_user_id");
    }
}