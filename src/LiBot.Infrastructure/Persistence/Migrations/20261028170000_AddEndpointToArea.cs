using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiBot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEndpointToArea : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "EndpointId",
                table: "AREA",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AREA_EndpointId",
                table: "AREA",
                column: "EndpointId");

            migrationBuilder.AddForeignKey(
                name: "FK_AREA_ENDPOINT_EndpointId",
                table: "AREA",
                column: "EndpointId",
                principalTable: "ENDPOINT",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AREA_ENDPOINT_EndpointId",
                table: "AREA");

            migrationBuilder.DropIndex(
                name: "IX_AREA_EndpointId",
                table: "AREA");

            migrationBuilder.DropColumn(
                name: "EndpointId",
                table: "AREA");
        }
    }
}
