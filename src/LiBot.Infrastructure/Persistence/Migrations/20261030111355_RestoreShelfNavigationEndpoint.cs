using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiBot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RestoreShelfNavigationEndpoint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Some installations already have this relationship from the legacy schema,
            // even when EF's migration history does not. Reconcile without replacing data.
            migrationBuilder.Sql("""
                IF COL_LENGTH(N'dbo.SHELF', N'EndpointId') IS NULL
                    ALTER TABLE [dbo].[SHELF] ADD [EndpointId] uniqueidentifier NULL;

                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE [name] = N'IX_SHELF_EndpointId'
                      AND [object_id] = OBJECT_ID(N'dbo.SHELF', N'U'))
                    CREATE INDEX [IX_SHELF_EndpointId] ON [dbo].[SHELF] ([EndpointId]);

                IF NOT EXISTS (
                    SELECT 1 FROM sys.foreign_keys
                    WHERE [name] = N'FK_SHELF_ENDPOINT_EndpointId'
                      AND [parent_object_id] = OBJECT_ID(N'dbo.SHELF', N'U'))
                    ALTER TABLE [dbo].[SHELF] WITH CHECK
                    ADD CONSTRAINT [FK_SHELF_ENDPOINT_EndpointId]
                        FOREIGN KEY ([EndpointId]) REFERENCES [dbo].[ENDPOINT] ([Id])
                        ON DELETE SET NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // This is a schema reconciliation for an existing legacy relationship.
            // Do not delete potentially pre-existing shelf-to-endpoint assignments on rollback.
        }
    }
}
