using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiBot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignDatabaseWithDomainNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AISLE_ENDPOINT_EndpointId",
                table: "AISLE");

            migrationBuilder.DropForeignKey(
                name: "FK_JOB_ENDPOINT_DestinationEndpointId",
                table: "JOB");

            migrationBuilder.DropForeignKey(
                name: "FK_JOB_ENDPOINT_SourceEndpointId",
                table: "JOB");

            migrationBuilder.DropForeignKey(
                name: "FK_JOB_TASK_ROBOT_RobotId",
                table: "JOB_TASK");

            migrationBuilder.DropTable(
                name: "SCHEDULE_JOBSTEP");

            migrationBuilder.DropTable(
                name: "SCHEDULE_JOBTASK");

            migrationBuilder.DropTable(
                name: "SCHEDULE_JOB");

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'dbo.REPORT', N'U') IS NOT NULL
                BEGIN
                    IF COL_LENGTH(N'dbo.INVENTORY_REPORT', N'JobId') IS NULL
                        ALTER TABLE dbo.INVENTORY_REPORT ADD JobId uniqueidentifier NULL;
                    IF COL_LENGTH(N'dbo.PATROL_REPORT', N'JobId') IS NULL
                        ALTER TABLE dbo.PATROL_REPORT ADD JobId uniqueidentifier NULL;
                    IF COL_LENGTH(N'dbo.INVENTORY_REPORT', N'Data') IS NULL
                        ALTER TABLE dbo.INVENTORY_REPORT ADD Data nvarchar(max) NULL;
                    IF COL_LENGTH(N'dbo.PATROL_REPORT', N'Data') IS NULL
                        ALTER TABLE dbo.PATROL_REPORT ADD Data nvarchar(max) NULL;

                    EXEC(N'UPDATE child SET Data = COALESCE(parent.Summary, N''{}'')
                          FROM dbo.INVENTORY_REPORT child
                          INNER JOIN dbo.REPORT parent ON parent.Id = child.ReportId');
                    EXEC(N'UPDATE child SET Data = COALESCE(parent.Summary, N''{}'')
                          FROM dbo.PATROL_REPORT child
                          INNER JOIN dbo.REPORT parent ON parent.Id = child.ReportId');

                    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_INVENTORY_REPORT_REPORT_ReportId')
                        ALTER TABLE dbo.INVENTORY_REPORT DROP CONSTRAINT FK_INVENTORY_REPORT_REPORT_ReportId;
                    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PATROL_REPORT_REPORT_ReportId')
                        ALTER TABLE dbo.PATROL_REPORT DROP CONSTRAINT FK_PATROL_REPORT_REPORT_ReportId;
                    IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.INVENTORY_REPORT') AND name = N'IX_INVENTORY_REPORT_ReportId')
                        DROP INDEX IX_INVENTORY_REPORT_ReportId ON dbo.INVENTORY_REPORT;
                    IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.PATROL_REPORT') AND name = N'IX_PATROL_REPORT_ReportId')
                        DROP INDEX IX_PATROL_REPORT_ReportId ON dbo.PATROL_REPORT;
                    IF COL_LENGTH(N'dbo.INVENTORY_REPORT', N'ReportId') IS NOT NULL
                        EXEC(N'ALTER TABLE dbo.INVENTORY_REPORT DROP COLUMN ReportId');
                    IF COL_LENGTH(N'dbo.PATROL_REPORT', N'ReportId') IS NOT NULL
                        EXEC(N'ALTER TABLE dbo.PATROL_REPORT DROP COLUMN ReportId');
                    DROP TABLE dbo.REPORT;
                END
                ELSE
                BEGIN
                    IF COL_LENGTH(N'dbo.INVENTORY_REPORT', N'JobId') IS NULL
                        ALTER TABLE dbo.INVENTORY_REPORT ADD JobId uniqueidentifier NULL;
                    IF COL_LENGTH(N'dbo.PATROL_REPORT', N'JobId') IS NULL
                        ALTER TABLE dbo.PATROL_REPORT ADD JobId uniqueidentifier NULL;
                    IF COL_LENGTH(N'dbo.INVENTORY_REPORT', N'Data') IS NULL
                        ALTER TABLE dbo.INVENTORY_REPORT ADD Data nvarchar(max) NULL;
                    IF COL_LENGTH(N'dbo.PATROL_REPORT', N'Data') IS NULL
                        ALTER TABLE dbo.PATROL_REPORT ADD Data nvarchar(max) NULL;

                    IF COL_LENGTH(N'dbo.INVENTORY_REPORT', N'Summary') IS NOT NULL
                        EXEC(N'UPDATE dbo.INVENTORY_REPORT SET Data = COALESCE(Summary, N''{}'')');
                    IF COL_LENGTH(N'dbo.PATROL_REPORT', N'Summary') IS NOT NULL
                        EXEC(N'UPDATE dbo.PATROL_REPORT SET Data = COALESCE(Summary, N''{}'')');
                    IF COL_LENGTH(N'dbo.INVENTORY_REPORT', N'ReportType') IS NOT NULL
                        EXEC(N'ALTER TABLE dbo.INVENTORY_REPORT DROP COLUMN ReportType');
                    IF COL_LENGTH(N'dbo.PATROL_REPORT', N'ReportType') IS NOT NULL
                        EXEC(N'ALTER TABLE dbo.PATROL_REPORT DROP COLUMN ReportType');
                    IF COL_LENGTH(N'dbo.INVENTORY_REPORT', N'Summary') IS NOT NULL
                        EXEC(N'ALTER TABLE dbo.INVENTORY_REPORT DROP COLUMN Summary');
                    IF COL_LENGTH(N'dbo.PATROL_REPORT', N'Summary') IS NOT NULL
                        EXEC(N'ALTER TABLE dbo.PATROL_REPORT DROP COLUMN Summary');
                END;

                IF COL_LENGTH(N'dbo.INVENTORY_REPORT', N'Data') IS NOT NULL
                BEGIN
                    EXEC(N'UPDATE dbo.INVENTORY_REPORT SET Data = N''{}'' WHERE Data IS NULL');
                    EXEC(N'ALTER TABLE dbo.INVENTORY_REPORT ALTER COLUMN Data nvarchar(max) NOT NULL');
                END;
                IF COL_LENGTH(N'dbo.PATROL_REPORT', N'Data') IS NOT NULL
                BEGIN
                    EXEC(N'UPDATE dbo.PATROL_REPORT SET Data = N''{}'' WHERE Data IS NULL');
                    EXEC(N'ALTER TABLE dbo.PATROL_REPORT ALTER COLUMN Data nvarchar(max) NOT NULL');
                END;

                IF COL_LENGTH(N'dbo.PATROL_REPORT', N'FallenBookCount') IS NOT NULL
                    ALTER TABLE dbo.PATROL_REPORT DROP COLUMN FallenBookCount;
                IF COL_LENGTH(N'dbo.PATROL_REPORT', N'UnknownObstacleCount') IS NOT NULL
                    ALTER TABLE dbo.PATROL_REPORT DROP COLUMN UnknownObstacleCount;
                IF COL_LENGTH(N'dbo.PATROL_REPORT', N'OtherWarningCount') IS NOT NULL
                BEGIN
                    EXEC(N'UPDATE dbo.PATROL_REPORT SET WarningCount = OtherWarningCount');
                    ALTER TABLE dbo.PATROL_REPORT DROP COLUMN OtherWarningCount;
                END;
                IF COL_LENGTH(N'dbo.PATROL_REPORT', N'PatrolStatus') IS NOT NULL
                    ALTER TABLE dbo.PATROL_REPORT DROP COLUMN PatrolStatus;

                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_INVENTORY_REPORT_JOB_JobId')
                    ALTER TABLE dbo.INVENTORY_REPORT DROP CONSTRAINT FK_INVENTORY_REPORT_JOB_JobId;
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PATROL_REPORT_JOB_JobId')
                    ALTER TABLE dbo.PATROL_REPORT DROP CONSTRAINT FK_PATROL_REPORT_JOB_JobId;
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.INVENTORY_REPORT') AND name = N'IX_INVENTORY_REPORT_JobId')
                    CREATE INDEX IX_INVENTORY_REPORT_JobId ON dbo.INVENTORY_REPORT(JobId);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.PATROL_REPORT') AND name = N'IX_PATROL_REPORT_JobId')
                    CREATE INDEX IX_PATROL_REPORT_JobId ON dbo.PATROL_REPORT(JobId);
                ALTER TABLE dbo.INVENTORY_REPORT ADD CONSTRAINT FK_INVENTORY_REPORT_JOB_JobId
                    FOREIGN KEY (JobId) REFERENCES dbo.JOB(Id) ON DELETE SET NULL;
                ALTER TABLE dbo.PATROL_REPORT ADD CONSTRAINT FK_PATROL_REPORT_JOB_JobId
                    FOREIGN KEY (JobId) REFERENCES dbo.JOB(Id) ON DELETE SET NULL;
                """);

            migrationBuilder.DropIndex(
                name: "IX_JOB_DestinationEndpointId",
                table: "JOB");

            migrationBuilder.DropIndex(
                name: "IX_JOB_SourceEndpointId",
                table: "JOB");

            migrationBuilder.DropIndex(
                name: "IX_JOB_TASK_RobotId",
                table: "JOB_TASK");

            migrationBuilder.DropIndex(
                name: "IX_AISLE_EndpointId",
                table: "AISLE");

            migrationBuilder.DropColumn(
                name: "DestinationEndpointId",
                table: "JOB");

            migrationBuilder.DropColumn(
                name: "EndpointId",
                table: "AISLE");

            migrationBuilder.DropColumn(
                name: "SourceEndpointId",
                table: "JOB");

            migrationBuilder.AddColumn<Guid>(
                name: "RobotId",
                table: "JOB",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE j
                SET RobotId = assigned.RobotId
                FROM JOB j
                CROSS APPLY (
                    SELECT TOP (1) jt.RobotId
                    FROM JOB_TASK jt
                    WHERE jt.JobId = j.Id AND jt.RobotId IS NOT NULL
                    ORDER BY jt.CreatedAtUtc
                ) assigned;
                """);

            migrationBuilder.DropColumn(
                name: "RobotId",
                table: "JOB_TASK");

            migrationBuilder.AddColumn<Guid>(
                name: "WorkflowTaskId",
                table: "JOB_TASK",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "ZONE",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "ZONE",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "WORKFLOW_TASK",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Data",
                table: "SCHEDULE",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "SCHEDULE",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "PositionX",
                table: "ROBOT_LOG",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "PositionY",
                table: "ROBOT_LOG",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Yaw",
                table: "ROBOT_LOG",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

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
                name: "Name",
                table: "JOB_TASK",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "Mission task");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "JOB_TASK",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "Pending");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "JOB_STEP",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "Pending");

            migrationBuilder.AddColumn<string>(
                name: "StepType",
                table: "JOB_STEP",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "UNKNOWN");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "FLOOR",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "ENDPOINT",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "BOOK",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE jt
                SET WorkflowTaskId = stepTask.WorkflowTaskId
                FROM JOB_TASK jt
                CROSS APPLY (
                    SELECT TOP (1) ts.WorkflowTaskId
                    FROM JOB_STEP js
                    INNER JOIN TASK_STEP ts ON ts.Id = js.TaskStepId
                    WHERE js.JobTaskId = jt.Id
                    ORDER BY js.Sequence
                ) stepTask;

                UPDATE jt
                SET Name = COALESCE(NULLIF(wt.Name, ''), NULLIF(j.Name, ''), 'Mission task'),
                    Status = CASE
                        WHEN jt.CompletedAtUtc IS NOT NULL THEN 'Completed'
                        WHEN jt.StartedAtUtc IS NOT NULL THEN 'Running'
                        ELSE 'Pending'
                    END
                FROM JOB_TASK jt
                INNER JOIN JOB j ON j.Id = jt.JobId
                LEFT JOIN WORKFLOW_TASK wt ON wt.Id = jt.WorkflowTaskId;

                UPDATE js
                SET StepType = ts.StepType
                FROM JOB_STEP js
                INNER JOIN TASK_STEP ts ON ts.Id = js.TaskStepId;

                UPDATE ZONE SET Name = ZoneType WHERE Name = '';
                UPDATE SCHEDULE SET Name = ScheduleType WHERE Name = '';
                UPDATE MAP
                SET FloorId = defaultFloor.Id
                FROM MAP
                CROSS APPLY (SELECT TOP (1) Id FROM FLOOR ORDER BY FloorNumber) defaultFloor
                WHERE FloorId IS NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_MAP_FloorId",
                table: "MAP",
                column: "FloorId");

            migrationBuilder.CreateIndex(
                name: "IX_JOB_RobotId",
                table: "JOB",
                column: "RobotId");

            migrationBuilder.CreateIndex(
                name: "IX_JOB_TASK_WorkflowTaskId",
                table: "JOB_TASK",
                column: "WorkflowTaskId");

            migrationBuilder.AddForeignKey(
                name: "FK_JOB_ROBOT_RobotId",
                table: "JOB",
                column: "RobotId",
                principalTable: "ROBOT",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_JOB_TASK_WORKFLOW_TASK_WorkflowTaskId",
                table: "JOB_TASK",
                column: "WorkflowTaskId",
                principalTable: "WORKFLOW_TASK",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_MAP_FLOOR_FloorId",
                table: "MAP",
                column: "FloorId",
                principalTable: "FLOOR",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_INVENTORY_REPORT_JOB_JobId",
                table: "INVENTORY_REPORT");

            migrationBuilder.DropForeignKey(
                name: "FK_JOB_ROBOT_RobotId",
                table: "JOB");

            migrationBuilder.DropForeignKey(
                name: "FK_JOB_TASK_WORKFLOW_TASK_WorkflowTaskId",
                table: "JOB_TASK");

            migrationBuilder.DropForeignKey(
                name: "FK_MAP_FLOOR_FloorId",
                table: "MAP");

            migrationBuilder.DropForeignKey(
                name: "FK_PATROL_REPORT_JOB_JobId",
                table: "PATROL_REPORT");

            migrationBuilder.DropIndex(
                name: "IX_PATROL_REPORT_JobId",
                table: "PATROL_REPORT");

            migrationBuilder.DropIndex(
                name: "IX_MAP_FloorId",
                table: "MAP");

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
                name: "Data",
                table: "SCHEDULE");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "SCHEDULE");

            migrationBuilder.DropColumn(
                name: "PositionX",
                table: "ROBOT_LOG");

            migrationBuilder.DropColumn(
                name: "PositionY",
                table: "ROBOT_LOG");

            migrationBuilder.DropColumn(
                name: "Yaw",
                table: "ROBOT_LOG");

            migrationBuilder.DropColumn(
                name: "JobId",
                table: "PATROL_REPORT");

            migrationBuilder.DropColumn(
                name: "FloorId",
                table: "MAP");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "MAP");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "JOB_TASK");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "JOB_TASK");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "JOB_STEP");

            migrationBuilder.DropColumn(
                name: "StepType",
                table: "JOB_STEP");

            migrationBuilder.DropColumn(
                name: "Data",
                table: "INVENTORY_REPORT");

            migrationBuilder.DropColumn(
                name: "JobId",
                table: "INVENTORY_REPORT");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "FLOOR");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "ENDPOINT");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "BOOK");

            migrationBuilder.RenameColumn(
                name: "Data",
                table: "PATROL_REPORT",
                newName: "PatrolStatus");

            migrationBuilder.RenameColumn(
                name: "WorkflowTaskId",
                table: "JOB_TASK",
                newName: "RobotId");

            migrationBuilder.RenameIndex(
                name: "IX_JOB_TASK_WorkflowTaskId",
                table: "JOB_TASK",
                newName: "IX_JOB_TASK_RobotId");

            migrationBuilder.RenameColumn(
                name: "RobotId",
                table: "JOB",
                newName: "SourceEndpointId");

            migrationBuilder.RenameIndex(
                name: "IX_JOB_RobotId",
                table: "JOB",
                newName: "IX_JOB_SourceEndpointId");

            migrationBuilder.AddColumn<int>(
                name: "FallenBookCount",
                table: "PATROL_REPORT",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OtherWarningCount",
                table: "PATROL_REPORT",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "ReportId",
                table: "PATROL_REPORT",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "UnknownObstacleCount",
                table: "PATROL_REPORT",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "DestinationEndpointId",
                table: "JOB",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReportId",
                table: "INVENTORY_REPORT",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "EndpointId",
                table: "AISLE",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "REPORT",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReportType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_REPORT", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SCHEDULE_JOB",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    JobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScheduleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SCHEDULE_JOB", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SCHEDULE_JOB_JOB_JobId",
                        column: x => x.JobId,
                        principalTable: "JOB",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SCHEDULE_JOB_SCHEDULE_ScheduleId",
                        column: x => x.ScheduleId,
                        principalTable: "SCHEDULE",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SCHEDULE_JOBTASK",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ScheduledJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    WorkflowTaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SCHEDULE_JOBTASK", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SCHEDULE_JOBTASK_SCHEDULE_JOB_ScheduledJobId",
                        column: x => x.ScheduledJobId,
                        principalTable: "SCHEDULE_JOB",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SCHEDULE_JOBTASK_WORKFLOW_TASK_WorkflowTaskId",
                        column: x => x.WorkflowTaskId,
                        principalTable: "WORKFLOW_TASK",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SCHEDULE_JOBSTEP",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Data = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Parameters = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ScheduledJobTaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SCHEDULE_JOBSTEP", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SCHEDULE_JOBSTEP_SCHEDULE_JOBTASK_ScheduledJobTaskId",
                        column: x => x.ScheduledJobTaskId,
                        principalTable: "SCHEDULE_JOBTASK",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PATROL_REPORT_ReportId",
                table: "PATROL_REPORT",
                column: "ReportId");

            migrationBuilder.CreateIndex(
                name: "IX_JOB_DestinationEndpointId",
                table: "JOB",
                column: "DestinationEndpointId");

            migrationBuilder.CreateIndex(
                name: "IX_INVENTORY_REPORT_ReportId",
                table: "INVENTORY_REPORT",
                column: "ReportId");

            migrationBuilder.CreateIndex(
                name: "IX_AISLE_EndpointId",
                table: "AISLE",
                column: "EndpointId");

            migrationBuilder.CreateIndex(
                name: "IX_SCHEDULE_JOB_JobId",
                table: "SCHEDULE_JOB",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_SCHEDULE_JOB_ScheduleId",
                table: "SCHEDULE_JOB",
                column: "ScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_SCHEDULE_JOBSTEP_ScheduledJobTaskId",
                table: "SCHEDULE_JOBSTEP",
                column: "ScheduledJobTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_SCHEDULE_JOBTASK_ScheduledJobId",
                table: "SCHEDULE_JOBTASK",
                column: "ScheduledJobId");

            migrationBuilder.CreateIndex(
                name: "IX_SCHEDULE_JOBTASK_WorkflowTaskId",
                table: "SCHEDULE_JOBTASK",
                column: "WorkflowTaskId");

            migrationBuilder.AddForeignKey(
                name: "FK_AISLE_ENDPOINT_EndpointId",
                table: "AISLE",
                column: "EndpointId",
                principalTable: "ENDPOINT",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_INVENTORY_REPORT_REPORT_ReportId",
                table: "INVENTORY_REPORT",
                column: "ReportId",
                principalTable: "REPORT",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_JOB_ENDPOINT_DestinationEndpointId",
                table: "JOB",
                column: "DestinationEndpointId",
                principalTable: "ENDPOINT",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_JOB_ENDPOINT_SourceEndpointId",
                table: "JOB",
                column: "SourceEndpointId",
                principalTable: "ENDPOINT",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_JOB_TASK_ROBOT_RobotId",
                table: "JOB_TASK",
                column: "RobotId",
                principalTable: "ROBOT",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PATROL_REPORT_REPORT_ReportId",
                table: "PATROL_REPORT",
                column: "ReportId",
                principalTable: "REPORT",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
