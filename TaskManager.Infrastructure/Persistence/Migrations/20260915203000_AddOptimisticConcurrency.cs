using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskManager.Infrastructure.Persistence.Migrations;

public partial class AddOptimisticConcurrency : Migration
{
    protected override void Up(
        MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<long>(
            name: "version",
            table: "projects",
            type: "bigint",
            nullable: false,
            defaultValue: 1L);

        migrationBuilder.AddColumn<long>(
            name: "version",
            table: "task_items",
            type: "bigint",
            nullable: false,
            defaultValue: 1L);
    }

    protected override void Down(
        MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "version",
            table: "projects");

        migrationBuilder.DropColumn(
            name: "version",
            table: "task_items");
    }
}
