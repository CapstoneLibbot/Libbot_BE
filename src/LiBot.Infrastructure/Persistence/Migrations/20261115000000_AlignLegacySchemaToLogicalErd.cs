using LiBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiBot.Infrastructure.Persistence.Migrations;

/// <summary>Moves the previously deployed LiBot schema to the corrected logical ERD.</summary>
[Migration("20261115000000_AlignLegacySchemaToLogicalErd")]
[DbContext(typeof(LiBotDbContext))]
public partial class AlignLegacySchemaToLogicalErd : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
DECLARE @HasLegacySchema bit = CASE WHEN COL_LENGTH(N'dbo.ACCOUNT', N'CreatedAtUtc') IS NOT NULL THEN 1 ELSE 0 END;
EXEC sys.sp_set_session_context @key = N'LiBotLegacySchema', @value = @HasLegacySchema;
""");

        foreach (var batch in MigrationSql.GetLegacyAlignmentBatches())
        {
            var escapedBatch = batch.Replace("'", "''", StringComparison.Ordinal);
            migrationBuilder.Sql($"IF TRY_CONVERT(int, SESSION_CONTEXT(N'LiBotLegacySchema')) = 1 EXEC sys.sp_executesql N'{escapedBatch}';");
        }

        migrationBuilder.Sql("EXEC sys.sp_set_session_context @key = N'LiBotLegacySchema', @value = NULL;");
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql("THROW 51020, 'Restore the pre-migration backup to reverse this data-preserving schema alignment.', 1;");
}
