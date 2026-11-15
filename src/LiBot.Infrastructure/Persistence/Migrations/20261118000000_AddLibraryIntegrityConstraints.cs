using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiBot.Infrastructure.Persistence.Migrations;

[Migration("20261118000000_AddLibraryIntegrityConstraints")]
public partial class AddLibraryIntegrityConstraints : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
IF EXISTS (SELECT Barcode FROM dbo.[BOOK_ITEM] GROUP BY Barcode HAVING COUNT(*) > 1)
    THROW 51029, 'BOOK_ITEM contains duplicate Barcode values. Resolve duplicates before applying this migration.', 1;
IF EXISTS (SELECT ShelfId, LevelNumber FROM dbo.[SHELF_LEVEL] GROUP BY ShelfId, LevelNumber HAVING COUNT(*) > 1)
    THROW 51030, 'SHELF_LEVEL contains duplicate (ShelfId, LevelNumber) values. Resolve duplicates before applying this migration.', 1;
CREATE UNIQUE INDEX [IX_BOOK_ITEM_Barcode] ON dbo.[BOOK_ITEM] ([Barcode]);
CREATE UNIQUE INDEX [IX_SHELF_LEVEL_ShelfId_LevelNumber] ON dbo.[SHELF_LEVEL] ([ShelfId], [LevelNumber]);
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql("DROP INDEX [IX_BOOK_ITEM_Barcode] ON dbo.[BOOK_ITEM]; DROP INDEX [IX_SHELF_LEVEL_ShelfId_LevelNumber] ON dbo.[SHELF_LEVEL];");
}
