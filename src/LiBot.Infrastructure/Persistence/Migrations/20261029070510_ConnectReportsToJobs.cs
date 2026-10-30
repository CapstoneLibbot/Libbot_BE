using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiBot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConnectReportsToJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "JobId",
                table: "PATROL_REPORT",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReportType",
                table: "PATROL_REPORT",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Summary",
                table: "PATROL_REPORT",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "JobId",
                table: "INVENTORY_REPORT",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReportType",
                table: "INVENTORY_REPORT",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Summary",
                table: "INVENTORY_REPORT",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE patrol
                SET patrol.Id = patrol.ReportId,
                    patrol.ReportType = report.ReportType,
                    patrol.Summary = report.Summary,
                    patrol.JobId = job.Id
                FROM PATROL_REPORT AS patrol
                INNER JOIN REPORT AS report ON report.Id = patrol.ReportId
                OUTER APPLY (
                    SELECT TRY_CONVERT(uniqueidentifier,
                        SUBSTRING(report.Summary,
                            CHARINDEX(N'mission ', report.Summary) + LEN(N'mission '), 36)) AS ParsedJobId
                ) AS parsed
                LEFT JOIN JOB AS job ON job.Id = parsed.ParsedJobId;

                UPDATE inventory
                SET inventory.Id = inventory.ReportId,
                    inventory.ReportType = report.ReportType,
                    inventory.Summary = report.Summary,
                    inventory.JobId = job.Id
                FROM INVENTORY_REPORT AS inventory
                INNER JOIN REPORT AS report ON report.Id = inventory.ReportId
                OUTER APPLY (
                    SELECT TRY_CONVERT(uniqueidentifier,
                        SUBSTRING(report.Summary,
                            CHARINDEX(N'mission ', report.Summary) + LEN(N'mission '), 36)) AS ParsedJobId
                ) AS parsed
                LEFT JOIN JOB AS job ON job.Id = parsed.ParsedJobId;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "ReportType",
                table: "PATROL_REPORT",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Summary",
                table: "PATROL_REPORT",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ReportType",
                table: "INVENTORY_REPORT",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Summary",
                table: "INVENTORY_REPORT",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.DropForeignKey(
                name: "FK_INVENTORY_REPORT_REPORT_ReportId",
                table: "INVENTORY_REPORT");

            migrationBuilder.DropForeignKey(
                name: "FK_PATROL_REPORT_REPORT_ReportId",
                table: "PATROL_REPORT");

            migrationBuilder.DropIndex(
                name: "IX_PATROL_REPORT_ReportId",
                table: "PATROL_REPORT");

            migrationBuilder.DropIndex(
                name: "IX_INVENTORY_REPORT_ReportId",
                table: "INVENTORY_REPORT");

            migrationBuilder.DropColumn(
                name: "ReportId",
                table: "PATROL_REPORT");

            migrationBuilder.DropColumn(
                name: "ReportId",
                table: "INVENTORY_REPORT");

            migrationBuilder.DropTable(
                name: "REPORT");

            migrationBuilder.CreateIndex(
                name: "IX_PATROL_REPORT_JobId",
                table: "PATROL_REPORT",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_INVENTORY_REPORT_JobId",
                table: "INVENTORY_REPORT",
                column: "JobId");

            migrationBuilder.AddForeignKey(
                name: "FK_INVENTORY_REPORT_JOB_JobId",
                table: "INVENTORY_REPORT",
                column: "JobId",
                principalTable: "JOB",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PATROL_REPORT_JOB_JobId",
                table: "PATROL_REPORT",
                column: "JobId",
                principalTable: "JOB",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_INVENTORY_REPORT_JOB_JobId",
                table: "INVENTORY_REPORT");

            migrationBuilder.DropForeignKey(
                name: "FK_PATROL_REPORT_JOB_JobId",
                table: "PATROL_REPORT");

            migrationBuilder.DropIndex(
                name: "IX_PATROL_REPORT_JobId",
                table: "PATROL_REPORT");

            migrationBuilder.DropIndex(
                name: "IX_INVENTORY_REPORT_JobId",
                table: "INVENTORY_REPORT");

            migrationBuilder.AddColumn<Guid>(
                name: "ReportId",
                table: "PATROL_REPORT",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReportId",
                table: "INVENTORY_REPORT",
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

            migrationBuilder.Sql("""
                INSERT INTO REPORT (Id, ReportType, Summary, CreatedAtUtc, UpdatedAtUtc)
                SELECT Id, ReportType, Summary, CreatedAtUtc, UpdatedAtUtc FROM PATROL_REPORT
                UNION ALL
                SELECT Id, ReportType, Summary, CreatedAtUtc, UpdatedAtUtc FROM INVENTORY_REPORT;

                UPDATE PATROL_REPORT SET ReportId = Id;
                UPDATE INVENTORY_REPORT SET ReportId = Id;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "ReportId",
                table: "PATROL_REPORT",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ReportId",
                table: "INVENTORY_REPORT",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "JobId",
                table: "PATROL_REPORT");

            migrationBuilder.DropColumn(
                name: "ReportType",
                table: "PATROL_REPORT");

            migrationBuilder.DropColumn(
                name: "Summary",
                table: "PATROL_REPORT");

            migrationBuilder.DropColumn(
                name: "JobId",
                table: "INVENTORY_REPORT");

            migrationBuilder.DropColumn(
                name: "ReportType",
                table: "INVENTORY_REPORT");

            migrationBuilder.DropColumn(
                name: "Summary",
                table: "INVENTORY_REPORT");

            migrationBuilder.CreateIndex(
                name: "IX_PATROL_REPORT_ReportId",
                table: "PATROL_REPORT",
                column: "ReportId");

            migrationBuilder.CreateIndex(
                name: "IX_INVENTORY_REPORT_ReportId",
                table: "INVENTORY_REPORT",
                column: "ReportId");

            migrationBuilder.AddForeignKey(
                name: "FK_INVENTORY_REPORT_REPORT_ReportId",
                table: "INVENTORY_REPORT",
                column: "ReportId",
                principalTable: "REPORT",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

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
