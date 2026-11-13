using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiBot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddErdRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_SCHEDULE_WorkflowId",
                table: "SCHEDULE",
                column: "WorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_ROBOT_LOG_MapId",
                table: "ROBOT_LOG",
                column: "MapId");

            migrationBuilder.CreateIndex(
                name: "IX_ROBOT_MapId",
                table: "ROBOT",
                column: "MapId");

            migrationBuilder.CreateIndex(
                name: "IX_JOB_TASK_WorkflowTaskId",
                table: "JOB_TASK",
                column: "WorkflowTaskId");

            migrationBuilder.AddForeignKey(
                name: "FK_JOB_TASK_WORKFLOW_TASK_WorkflowTaskId",
                table: "JOB_TASK",
                column: "WorkflowTaskId",
                principalTable: "WORKFLOW_TASK",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ROBOT_MAP_MapId",
                table: "ROBOT",
                column: "MapId",
                principalTable: "MAP",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ROBOT_LOG_MAP_MapId",
                table: "ROBOT_LOG",
                column: "MapId",
                principalTable: "MAP",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SCHEDULE_WORKFLOW_WorkflowId",
                table: "SCHEDULE",
                column: "WorkflowId",
                principalTable: "WORKFLOW",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JOB_TASK_WORKFLOW_TASK_WorkflowTaskId",
                table: "JOB_TASK");

            migrationBuilder.DropForeignKey(
                name: "FK_ROBOT_MAP_MapId",
                table: "ROBOT");

            migrationBuilder.DropForeignKey(
                name: "FK_ROBOT_LOG_MAP_MapId",
                table: "ROBOT_LOG");

            migrationBuilder.DropForeignKey(
                name: "FK_SCHEDULE_WORKFLOW_WorkflowId",
                table: "SCHEDULE");

            migrationBuilder.DropIndex(
                name: "IX_SCHEDULE_WorkflowId",
                table: "SCHEDULE");

            migrationBuilder.DropIndex(
                name: "IX_ROBOT_LOG_MapId",
                table: "ROBOT_LOG");

            migrationBuilder.DropIndex(
                name: "IX_ROBOT_MapId",
                table: "ROBOT");

            migrationBuilder.DropIndex(
                name: "IX_JOB_TASK_WorkflowTaskId",
                table: "JOB_TASK");
        }
    }
}
