using LiBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiBot.Infrastructure.Persistence.Migrations;

[DbContext(typeof(LiBotDbContext))]
[Migration("20261019100000_AddWorkflowRuntimeState")]
public partial class AddWorkflowRuntimeState : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "RuntimeId",
            table: "JOB_TASK",
            type: "nvarchar(32)",
            maxLength: 32,
            nullable: false,
            defaultValue: "real");

        migrationBuilder.AddColumn<string>(
            name: "ExecutionId",
            table: "JOB_TASK",
            type: "nvarchar(64)",
            maxLength: 64,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ExecutionState",
            table: "JOB_TASK",
            type: "nvarchar(max)",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "ExecutionState", table: "JOB_TASK");
        migrationBuilder.DropColumn(name: "ExecutionId", table: "JOB_TASK");
        migrationBuilder.DropColumn(name: "RuntimeId", table: "JOB_TASK");
    }
}
