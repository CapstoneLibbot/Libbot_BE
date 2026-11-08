using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiBot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateFloorAndAisleStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AISLE_ENDPOINT_EndpointId",
                table: "AISLE");

            migrationBuilder.DropIndex(
                name: "IX_AISLE_EndpointId",
                table: "AISLE");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "AISLE");

            migrationBuilder.DropColumn(
                name: "EndpointId",
                table: "AISLE");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "FLOOR",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "FLOOR",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Active");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "AISLE",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Active");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "FLOOR");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "FLOOR");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "AISLE");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "AISLE",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "EndpointId",
                table: "AISLE",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AISLE_EndpointId",
                table: "AISLE",
                column: "EndpointId");

            migrationBuilder.AddForeignKey(
                name: "FK_AISLE_ENDPOINT_EndpointId",
                table: "AISLE",
                column: "EndpointId",
                principalTable: "ENDPOINT",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
