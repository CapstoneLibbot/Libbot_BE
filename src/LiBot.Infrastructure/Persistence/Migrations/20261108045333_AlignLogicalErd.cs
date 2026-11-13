using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiBot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignLogicalErd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Code",
                table: "SHELF");

            migrationBuilder.RenameColumn(
                name: "Image",
                table: "BOOK",
                newName: "ImageUrl");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "ZONE",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "ZONE",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "WORKFLOW_TASK",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "WORKFLOW",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "WORKFLOW",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "Active");

            migrationBuilder.AddColumn<int>(
                name: "SequenceNumber",
                table: "TASK_STEP",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "SHELF_LEVEL",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "SHELF_LEVEL",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "Active");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "SHELF",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EndpointId",
                table: "SHELF",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "SHELF",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Active");

            migrationBuilder.AddColumn<string>(
                name: "Data",
                table: "SCHEDULE",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "SCHEDULE",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "SCHEDULE",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WorkflowId",
                table: "SCHEDULE",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Data",
                table: "ROBOT_LOG",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MapId",
                table: "ROBOT_LOG",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "PositionX",
                table: "ROBOT_LOG",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "PositionY",
                table: "ROBOT_LOG",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "ROBOT_LOG",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "ROBOT",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<Guid>(
                name: "MapId",
                table: "ROBOT",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "ROBOT",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAtUtc",
                table: "PATROL_REPORT",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Data",
                table: "PATROL_REPORT",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "JobId",
                table: "PATROL_REPORT",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAtUtc",
                table: "PATROL_REPORT",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FloorId",
                table: "MAP",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "MAP",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "Active");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "JOB_TASK",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "JOB_TASK",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "JOB_TASK",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WorkflowTaskId",
                table: "JOB_TASK",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "JOB_STEP",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "JOB_STEP",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StepType",
                table: "JOB_STEP",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RobotId",
                table: "JOB",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Data",
                table: "INVENTORY_REPORT",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FloorId",
                table: "INVENTORY_REPORT",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "JobId",
                table: "INVENTORY_REPORT",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "ENDPOINT_GROUP",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "ENDPOINT",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EndpointType",
                table: "ENDPOINT",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "BOOK",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RoleId",
                table: "ACCOUNT",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "ACCOUNT",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SHELF_EndpointId",
                table: "SHELF",
                column: "EndpointId");

            migrationBuilder.CreateIndex(
                name: "IX_PATROL_REPORT_JobId",
                table: "PATROL_REPORT",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_MAP_FloorId",
                table: "MAP",
                column: "FloorId");

            migrationBuilder.CreateIndex(
                name: "IX_JOB_RobotId",
                table: "JOB",
                column: "RobotId");

            migrationBuilder.CreateIndex(
                name: "IX_INVENTORY_REPORT_FloorId",
                table: "INVENTORY_REPORT",
                column: "FloorId");

            migrationBuilder.CreateIndex(
                name: "IX_INVENTORY_REPORT_JobId",
                table: "INVENTORY_REPORT",
                column: "JobId");

            migrationBuilder.AddForeignKey(
                name: "FK_INVENTORY_REPORT_FLOOR_FloorId",
                table: "INVENTORY_REPORT",
                column: "FloorId",
                principalTable: "FLOOR",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_INVENTORY_REPORT_JOB_JobId",
                table: "INVENTORY_REPORT",
                column: "JobId",
                principalTable: "JOB",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_JOB_ROBOT_RobotId",
                table: "JOB",
                column: "RobotId",
                principalTable: "ROBOT",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_MAP_FLOOR_FloorId",
                table: "MAP",
                column: "FloorId",
                principalTable: "FLOOR",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PATROL_REPORT_JOB_JobId",
                table: "PATROL_REPORT",
                column: "JobId",
                principalTable: "JOB",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SHELF_ENDPOINT_EndpointId",
                table: "SHELF",
                column: "EndpointId",
                principalTable: "ENDPOINT",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_INVENTORY_REPORT_FLOOR_FloorId",
                table: "INVENTORY_REPORT");

            migrationBuilder.DropForeignKey(
                name: "FK_INVENTORY_REPORT_JOB_JobId",
                table: "INVENTORY_REPORT");

            migrationBuilder.DropForeignKey(
                name: "FK_JOB_ROBOT_RobotId",
                table: "JOB");

            migrationBuilder.DropForeignKey(
                name: "FK_MAP_FLOOR_FloorId",
                table: "MAP");

            migrationBuilder.DropForeignKey(
                name: "FK_PATROL_REPORT_JOB_JobId",
                table: "PATROL_REPORT");

            migrationBuilder.DropForeignKey(
                name: "FK_SHELF_ENDPOINT_EndpointId",
                table: "SHELF");

            migrationBuilder.DropIndex(
                name: "IX_SHELF_EndpointId",
                table: "SHELF");

            migrationBuilder.DropIndex(
                name: "IX_PATROL_REPORT_JobId",
                table: "PATROL_REPORT");

            migrationBuilder.DropIndex(
                name: "IX_MAP_FloorId",
                table: "MAP");

            migrationBuilder.DropIndex(
                name: "IX_JOB_RobotId",
                table: "JOB");

            migrationBuilder.DropIndex(
                name: "IX_INVENTORY_REPORT_FloorId",
                table: "INVENTORY_REPORT");

            migrationBuilder.DropIndex(
                name: "IX_INVENTORY_REPORT_JobId",
                table: "INVENTORY_REPORT");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "ZONE");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "ZONE");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "WORKFLOW_TASK");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "WORKFLOW");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "WORKFLOW");

            migrationBuilder.DropColumn(
                name: "SequenceNumber",
                table: "TASK_STEP");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "SHELF_LEVEL");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "SHELF_LEVEL");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "SHELF");

            migrationBuilder.DropColumn(
                name: "EndpointId",
                table: "SHELF");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "SHELF");

            migrationBuilder.DropColumn(
                name: "Data",
                table: "SCHEDULE");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "SCHEDULE");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "SCHEDULE");

            migrationBuilder.DropColumn(
                name: "WorkflowId",
                table: "SCHEDULE");

            migrationBuilder.DropColumn(
                name: "Data",
                table: "ROBOT_LOG");

            migrationBuilder.DropColumn(
                name: "MapId",
                table: "ROBOT_LOG");

            migrationBuilder.DropColumn(
                name: "PositionX",
                table: "ROBOT_LOG");

            migrationBuilder.DropColumn(
                name: "PositionY",
                table: "ROBOT_LOG");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "ROBOT_LOG");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "ROBOT");

            migrationBuilder.DropColumn(
                name: "MapId",
                table: "ROBOT");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "ROBOT");

            migrationBuilder.DropColumn(
                name: "CompletedAtUtc",
                table: "PATROL_REPORT");

            migrationBuilder.DropColumn(
                name: "Data",
                table: "PATROL_REPORT");

            migrationBuilder.DropColumn(
                name: "JobId",
                table: "PATROL_REPORT");

            migrationBuilder.DropColumn(
                name: "StartedAtUtc",
                table: "PATROL_REPORT");

            migrationBuilder.DropColumn(
                name: "FloorId",
                table: "MAP");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "MAP");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "JOB_TASK");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "JOB_TASK");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "JOB_TASK");

            migrationBuilder.DropColumn(
                name: "WorkflowTaskId",
                table: "JOB_TASK");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "JOB_STEP");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "JOB_STEP");

            migrationBuilder.DropColumn(
                name: "StepType",
                table: "JOB_STEP");

            migrationBuilder.DropColumn(
                name: "RobotId",
                table: "JOB");

            migrationBuilder.DropColumn(
                name: "Data",
                table: "INVENTORY_REPORT");

            migrationBuilder.DropColumn(
                name: "FloorId",
                table: "INVENTORY_REPORT");

            migrationBuilder.DropColumn(
                name: "JobId",
                table: "INVENTORY_REPORT");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "ENDPOINT_GROUP");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "ENDPOINT");

            migrationBuilder.DropColumn(
                name: "EndpointType",
                table: "ENDPOINT");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "BOOK");

            migrationBuilder.DropColumn(
                name: "RoleId",
                table: "ACCOUNT");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "ACCOUNT");

            migrationBuilder.RenameColumn(
                name: "ImageUrl",
                table: "BOOK",
                newName: "Image");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "SHELF",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
