using LiBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiBot.Infrastructure.Persistence.Migrations;

/// <summary>Clean SQL Server baseline generated from the corrected logical ERD.</summary>
[Migration("20261112000000_LogicalErdInitial")]
[DbContext(typeof(LiBotDbContext))]
public partial class LogicalErdInitial : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(MigrationSql.GetCreateTablesIfMissing());
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE dbo.[BOOK_ISSUE];");
        migrationBuilder.Sql("DROP TABLE dbo.[BOOK_ISSUE_DETAIL];");
        migrationBuilder.Sql("DROP TABLE dbo.[INVENTORY_REPORT_DETAIL];");
        migrationBuilder.Sql("DROP TABLE dbo.[BOOK_RECEIPT];");
        migrationBuilder.Sql("DROP TABLE dbo.[BOOK_RECEIPT_DETAIL];");
        migrationBuilder.Sql("DROP TABLE dbo.[SCHEDULE];");
        migrationBuilder.Sql("DROP TABLE dbo.[JOB_STEP];");
        migrationBuilder.Sql("DROP TABLE dbo.[PATROL_REPORT];");
        migrationBuilder.Sql("DROP TABLE dbo.[ENDPOINT_GROUP];");
        migrationBuilder.Sql("DROP TABLE dbo.[ENDPOINT_GROUP_MEMBER];");
        migrationBuilder.Sql("DROP TABLE dbo.[INVENTORY_REPORT];");
        migrationBuilder.Sql("DROP TABLE dbo.[JOB];");
        migrationBuilder.Sql("DROP TABLE dbo.[MAP];");
        migrationBuilder.Sql("DROP TABLE dbo.[JOB_TASK];");
        migrationBuilder.Sql("DROP TABLE dbo.[ROBOT_LOG];");
        migrationBuilder.Sql("DROP TABLE dbo.[TASK_STEP];");
        migrationBuilder.Sql("DROP TABLE dbo.[WORKFLOW_TASK];");
        migrationBuilder.Sql("DROP TABLE dbo.[WORKFLOW];");
        migrationBuilder.Sql("DROP TABLE dbo.[ROBOT];");
        migrationBuilder.Sql("DROP TABLE dbo.[JOB_EVENT];");
        migrationBuilder.Sql("DROP TABLE dbo.[FEEDBACK];");
        migrationBuilder.Sql("DROP TABLE dbo.[ENDPOINT];");
        migrationBuilder.Sql("DROP TABLE dbo.[BOOK_CATEGORY];");
        migrationBuilder.Sql("DROP TABLE dbo.[SHELF];");
        migrationBuilder.Sql("DROP TABLE dbo.[ZONE];");
        migrationBuilder.Sql("DROP TABLE dbo.[SHELF_LEVEL];");
        migrationBuilder.Sql("DROP TABLE dbo.[FLOOR];");
        migrationBuilder.Sql("DROP TABLE dbo.[BOOK];");
        migrationBuilder.Sql("DROP TABLE dbo.[AISLE];");
        migrationBuilder.Sql("DROP TABLE dbo.[BOOK_ITEM];");
        migrationBuilder.Sql("DROP TABLE dbo.[NOTIFICATION];");
        migrationBuilder.Sql("DROP TABLE dbo.[AUDIT_LOG];");
        migrationBuilder.Sql("DROP TABLE dbo.[ACCOUNT];");
    }
}
