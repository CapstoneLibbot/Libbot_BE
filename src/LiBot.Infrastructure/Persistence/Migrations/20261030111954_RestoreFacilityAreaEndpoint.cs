using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiBot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RestoreFacilityAreaEndpoint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Legacy installations can already have these columns outside EF's history.
            // Keep existing endpoint types and area assignments intact.
            migrationBuilder.Sql("""
                IF COL_LENGTH(N'dbo.ENDPOINT', N'EndpointType') IS NULL
                    ALTER TABLE [dbo].[ENDPOINT]
                    ADD [EndpointType] nvarchar(max) NOT NULL
                        CONSTRAINT [DF_ENDPOINT_EndpointType] DEFAULT N'WAYPOINT' WITH VALUES;

                IF COL_LENGTH(N'dbo.AREA', N'EndpointId') IS NULL
                    ALTER TABLE [dbo].[AREA] ADD [EndpointId] uniqueidentifier NULL;

                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE [name] = N'IX_AREA_EndpointId'
                      AND [object_id] = OBJECT_ID(N'dbo.AREA', N'U'))
                    CREATE INDEX [IX_AREA_EndpointId] ON [dbo].[AREA] ([EndpointId]);

                IF NOT EXISTS (
                    SELECT 1 FROM sys.foreign_keys
                    WHERE [name] = N'FK_AREA_ENDPOINT_EndpointId'
                      AND [parent_object_id] = OBJECT_ID(N'dbo.AREA', N'U'))
                    ALTER TABLE [dbo].[AREA] WITH CHECK
                    ADD CONSTRAINT [FK_AREA_ENDPOINT_EndpointId]
                        FOREIGN KEY ([EndpointId]) REFERENCES [dbo].[ENDPOINT] ([Id])
                        ON DELETE SET NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Preserve facility-to-endpoint relationships and endpoint classifications
            // that may have existed before EF began tracking this schema.
        }
    }
}
