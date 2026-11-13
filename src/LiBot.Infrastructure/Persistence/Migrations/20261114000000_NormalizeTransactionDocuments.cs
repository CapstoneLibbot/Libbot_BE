using LiBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiBot.Infrastructure.Persistence.Migrations;

/// <summary>Makes receipt and issue headers multi-book documents and enforces detail consistency.</summary>
[Migration("20261114000000_NormalizeTransactionDocuments")]
[DbContext(typeof(LiBotDbContext))]
public partial class NormalizeTransactionDocuments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // The logical ERD baseline and the following alignment migration already create
        // these detail keys in their final shape. Retain this migration id for databases
        // that recorded it before the baseline was consolidated.
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // See Up. No schema change is performed by this compatibility migration.
    }
}
