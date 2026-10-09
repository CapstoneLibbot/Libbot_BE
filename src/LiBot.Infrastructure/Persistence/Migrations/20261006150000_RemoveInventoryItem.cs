using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiBot.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
[Migration("20261006150000_RemoveInventoryItem")]
public partial class RemoveInventoryItem : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "INVENTORY_ITEM");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "INVENTORY_ITEM",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Quantity = table.Column<int>(type: "int", nullable: false),
                Unit = table.Column<string>(type: "nvarchar(max)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_INVENTORY_ITEM", x => x.Id);
            });
    }
}
