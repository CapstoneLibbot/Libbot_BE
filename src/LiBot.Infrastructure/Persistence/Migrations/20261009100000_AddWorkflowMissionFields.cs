using LiBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiBot.Infrastructure.Persistence.Migrations;

[DbContext(typeof(LiBotDbContext))]
[Migration("20261009100000_AddWorkflowMissionFields")]
public partial class AddWorkflowMissionFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(name: "WorkflowId", table: "JOB", type: "uniqueidentifier", nullable: true);
        migrationBuilder.AddColumn<string>(name: "Type", table: "WORKFLOW", type: "nvarchar(max)", nullable: false, defaultValue: "GENERAL");
        migrationBuilder.AddColumn<bool>(name: "IsActive", table: "WORKFLOW", type: "bit", nullable: false, defaultValue: true);
        migrationBuilder.AddColumn<string>(name: "Configuration", table: "TASK_STEP", type: "nvarchar(max)", nullable: true);
        migrationBuilder.CreateIndex(name: "IX_JOB_WorkflowId", table: "JOB", column: "WorkflowId");
        migrationBuilder.AddForeignKey(name: "FK_JOB_WORKFLOW_WorkflowId", table: "JOB", column: "WorkflowId",
            principalTable: "WORKFLOW", principalColumn: "Id", onDelete: ReferentialAction.SetNull);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(name: "FK_JOB_WORKFLOW_WorkflowId", table: "JOB");
        migrationBuilder.DropIndex(name: "IX_JOB_WorkflowId", table: "JOB");
        migrationBuilder.DropColumn(name: "WorkflowId", table: "JOB");
        migrationBuilder.DropColumn(name: "Type", table: "WORKFLOW");
        migrationBuilder.DropColumn(name: "IsActive", table: "WORKFLOW");
        migrationBuilder.DropColumn(name: "Configuration", table: "TASK_STEP");
    }
}
