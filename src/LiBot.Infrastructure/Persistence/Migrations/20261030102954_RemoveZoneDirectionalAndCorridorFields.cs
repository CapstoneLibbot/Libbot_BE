using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiBot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveZoneDirectionalAndCorridorFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowTurnaround",
                table: "ZONE");

            migrationBuilder.DropColumn(
                name: "CorridorWidth",
                table: "ZONE");

            migrationBuilder.DropColumn(
                name: "PreferredDirection",
                table: "ZONE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowTurnaround",
                table: "ZONE",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "CorridorWidth",
                table: "ZONE",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreferredDirection",
                table: "ZONE",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
