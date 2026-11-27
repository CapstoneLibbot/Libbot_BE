using LiBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiBot.Infrastructure.Persistence.Migrations;

[Migration("20261125000000_AlignBookImageColumn")]
[DbContext(typeof(LiBotDbContext))]
public sealed class AlignBookImageColumn : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
IF OBJECT_ID(N'dbo.BOOK', N'U') IS NULL
    THROW 51050, 'The BOOK table is missing; align the logical ERD schema before applying this migration.', 1;

IF COL_LENGTH(N'dbo.BOOK', N'image_url') IS NULL
BEGIN
    DECLARE @SourceColumn sysname = NULL;
    IF COL_LENGTH(N'dbo.BOOK', N'ImageUrl') IS NOT NULL SET @SourceColumn = N'ImageUrl';
    ELSE IF COL_LENGTH(N'dbo.BOOK', N'Image') IS NOT NULL SET @SourceColumn = N'Image';
    ELSE IF COL_LENGTH(N'dbo.BOOK', N'image') IS NOT NULL SET @SourceColumn = N'image';

    IF @SourceColumn IS NOT NULL
    BEGIN
        DECLARE @QualifiedColumn nvarchar(517) = N'dbo.BOOK.' + QUOTENAME(@SourceColumn);
        EXEC sys.sp_rename @objname = @QualifiedColumn, @newname = N'image_url', @objtype = N'COLUMN';
    END
    ELSE
        ALTER TABLE dbo.BOOK ADD image_url nvarchar(max) NULL;
END;
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql("THROW 51051, 'Restore the database backup to reverse the BOOK image column alignment.', 1;");
}
