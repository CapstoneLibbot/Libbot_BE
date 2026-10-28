using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiBot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEndpointTypesAndNavigationZones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE [ZONE] SET [ZoneType] = N'KEEP_OUT' WHERE [ZoneType] = N'EXCLUSION'");
            migrationBuilder.Sql("DELETE FROM [ZONE] WHERE [ZoneType] IN (N'PREFERRED_DIRECTION', N'NARROW_CORRIDOR')");

            migrationBuilder.DropColumn(
                name: "AllowTurnaround",
                table: "ZONE");

            migrationBuilder.DropColumn(
                name: "CorridorWidth",
                table: "ZONE");

            migrationBuilder.DropColumn(
                name: "PreferredDirection",
                table: "ZONE");

            migrationBuilder.AddColumn<string>(
                name: "EndpointType",
                table: "ENDPOINT",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "WAYPOINT");

            migrationBuilder.Sql("UPDATE [ENDPOINT] SET [EndpointType] = N'WAYPOINT'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE [ZONE] SET [ZoneType] = N'EXCLUSION' WHERE [ZoneType] = N'KEEP_OUT'");
            migrationBuilder.DropColumn(
                name: "EndpointType",
                table: "ENDPOINT");

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
