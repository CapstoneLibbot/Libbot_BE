using LiBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiBot.Infrastructure.Persistence.Migrations;

[DbContext(typeof(LiBotDbContext))]
[Migration("20261028150000_AddEndpointToShelf")]
public partial class AddEndpointToShelf : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "EndpointId",
            table: "SHELF",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_SHELF_EndpointId",
            table: "SHELF",
            column: "EndpointId");

        migrationBuilder.AddForeignKey(
            name: "FK_SHELF_ENDPOINT_EndpointId",
            table: "SHELF",
            column: "EndpointId",
            principalTable: "ENDPOINT",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);

        migrationBuilder.Sql("""
            UPDATE [shelf]
            SET [EndpointId] = [aisle].[EndpointId]
            FROM [SHELF] AS [shelf]
            INNER JOIN [AISLE] AS [aisle] ON [shelf].[AisleId] = [aisle].[Id]
            WHERE [aisle].[EndpointId] IS NOT NULL
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_SHELF_ENDPOINT_EndpointId",
            table: "SHELF");

        migrationBuilder.DropIndex(
            name: "IX_SHELF_EndpointId",
            table: "SHELF");

        migrationBuilder.DropColumn(
            name: "EndpointId",
            table: "SHELF");
    }
}
