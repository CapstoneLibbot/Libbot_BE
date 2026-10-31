using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiBot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrientedZoneGeometry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Height",
                table: "ZONE",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "RotationAngle",
                table: "ZONE",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Width",
                table: "ZONE",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Height",
                table: "ZONE");

            migrationBuilder.DropColumn(
                name: "RotationAngle",
                table: "ZONE");

            migrationBuilder.DropColumn(
                name: "Width",
                table: "ZONE");
        }
    }
}
