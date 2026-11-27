using LiBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiBot.Infrastructure.Persistence.Migrations;

[Migration("20261116000000_RemoveNonErdAuditColumns")]
[DbContext(typeof(LiBotDbContext))]
public partial class RemoveNonErdAuditColumns : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
DECLARE @DropDefaults nvarchar(max) = N'';
SELECT @DropDefaults = STRING_AGG(
    N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(dc.parent_object_id)) + N'.'
        + QUOTENAME(OBJECT_NAME(dc.parent_object_id)) + N' DROP CONSTRAINT ' + QUOTENAME(dc.name) + N';',
    CHAR(10))
FROM sys.default_constraints AS dc
JOIN sys.columns AS c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
JOIN sys.tables AS t ON t.object_id = dc.parent_object_id
WHERE t.name IN (N'BOOK_CATEGORY', N'ENDPOINT', N'MAP')
  AND c.name IN (N'CreatedAtUtc', N'UpdatedAtUtc');
IF @DropDefaults <> N'' EXEC sys.sp_executesql @DropDefaults;

DECLARE @DropColumns nvarchar(max) = N'';
SELECT @DropColumns = STRING_AGG(
    N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(c.object_id)) + N'.'
        + QUOTENAME(OBJECT_NAME(c.object_id)) + N' DROP COLUMN ' + QUOTENAME(c.name) + N';',
    CHAR(10))
FROM sys.columns AS c
JOIN sys.tables AS t ON t.object_id = c.object_id
WHERE t.name IN (N'BOOK_CATEGORY', N'ENDPOINT', N'MAP')
  AND c.name IN (N'CreatedAtUtc', N'UpdatedAtUtc');
IF @DropColumns <> N'' EXEC sys.sp_executesql @DropColumns;
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql("THROW 51025, 'Restore the pre-migration backup to recover removed audit columns.', 1;");
}
