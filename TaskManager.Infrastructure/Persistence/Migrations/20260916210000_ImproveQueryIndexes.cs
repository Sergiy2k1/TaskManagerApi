using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskManager.Infrastructure.Persistence.Migrations;

public partial class ImproveQueryIndexes : Migration
{
    protected override void Up(
        MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_task_items_project_id",
            table: "task_items");

        migrationBuilder.DropIndex(
            name: "ix_task_comments_task_item_id",
            table: "task_comments");

        migrationBuilder.CreateIndex(
            name: "ix_task_items_project_id_created_at_utc_id",
            table: "task_items",
            columns: new[]
            {
                "project_id",
                "created_at_utc",
                "id"
            });

        migrationBuilder.CreateIndex(
            name: "ix_task_comments_task_item_id_created_at_utc_id",
            table: "task_comments",
            columns: new[]
            {
                "task_item_id",
                "created_at_utc",
                "id"
            });

        migrationBuilder.CreateIndex(
            name: "ix_project_members_active_project_joined_user",
            table: "project_members",
            columns: new[]
            {
                "project_id",
                "joined_at_utc",
                "user_id"
            },
            filter: "removed_at_utc IS NULL");
    }

    protected override void Down(
        MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_project_members_active_project_joined_user",
            table: "project_members");

        migrationBuilder.DropIndex(
            name: "ix_task_comments_task_item_id_created_at_utc_id",
            table: "task_comments");

        migrationBuilder.DropIndex(
            name: "ix_task_items_project_id_created_at_utc_id",
            table: "task_items");

        migrationBuilder.CreateIndex(
            name: "ix_task_comments_task_item_id",
            table: "task_comments",
            column: "task_item_id");

        migrationBuilder.CreateIndex(
            name: "ix_task_items_project_id",
            table: "task_items",
            column: "project_id");
    }
}
