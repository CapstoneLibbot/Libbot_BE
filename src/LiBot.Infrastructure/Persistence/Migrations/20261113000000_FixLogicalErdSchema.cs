using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiBot.Infrastructure.Persistence.Migrations;

/// <summary>Corrects baseline generation defects without discarding existing data.</summary>
[Migration("20261113000000_FixLogicalErdSchema")]
public partial class FixLogicalErdSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
IF COL_LENGTH(N'dbo.ZONE', N'MaxSpeed     ') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM dbo.ZONE WHERE TRY_CONVERT(float, [MaxSpeed     ]) IS NULL)
        THROW 51000, 'ZONE.MaxSpeed contains a value that cannot be converted to float.', 1;
    EXEC sp_rename N'dbo.ZONE.[MaxSpeed     ]', N'MaxSpeed', N'COLUMN';
    ALTER TABLE dbo.ZONE ALTER COLUMN MaxSpeed float NOT NULL;
END;

IF EXISTS (SELECT 1 FROM dbo.SCHEDULE WHERE TRY_CONVERT(date, StartDate) IS NULL)
    THROW 51001, 'SCHEDULE.StartDate contains a value that cannot be converted to date.', 1;
IF EXISTS (SELECT 1 FROM dbo.SCHEDULE WHERE EndDate IS NOT NULL AND TRY_CONVERT(date, EndDate) IS NULL)
    THROW 51002, 'SCHEDULE.EndDate contains a value that cannot be converted to date.', 1;
ALTER TABLE dbo.SCHEDULE ALTER COLUMN StartDate date NOT NULL;
ALTER TABLE dbo.SCHEDULE ALTER COLUMN EndDate date NULL;

IF COL_LENGTH(N'dbo.ENDPOINT_GROUP_MEMBER', N'EnpointGroupId') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ENDPOINT_GROUP_MEMBER_ENDPOINT_GROUP_EnpointGroupId')
        ALTER TABLE dbo.ENDPOINT_GROUP_MEMBER DROP CONSTRAINT FK_ENDPOINT_GROUP_MEMBER_ENDPOINT_GROUP_EnpointGroupId;
    EXEC sp_rename N'dbo.ENDPOINT_GROUP_MEMBER.EnpointGroupId', N'EndpointGroupId', N'COLUMN';
    ALTER TABLE dbo.ENDPOINT_GROUP_MEMBER ADD CONSTRAINT FK_ENDPOINT_GROUP_MEMBER_ENDPOINT_GROUP_EndpointGroupId
        FOREIGN KEY (EndpointGroupId) REFERENCES dbo.ENDPOINT_GROUP(Id) ON DELETE NO ACTION;
END;
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ENDPOINT_GROUP_MEMBER_ENDPOINT_GROUP_EndpointGroupId')
    ALTER TABLE dbo.ENDPOINT_GROUP_MEMBER DROP CONSTRAINT FK_ENDPOINT_GROUP_MEMBER_ENDPOINT_GROUP_EndpointGroupId;
IF COL_LENGTH(N'dbo.ENDPOINT_GROUP_MEMBER', N'EndpointGroupId') IS NOT NULL
BEGIN
    EXEC sp_rename N'dbo.ENDPOINT_GROUP_MEMBER.EndpointGroupId', N'EnpointGroupId', N'COLUMN';
    ALTER TABLE dbo.ENDPOINT_GROUP_MEMBER ADD CONSTRAINT FK_ENDPOINT_GROUP_MEMBER_ENDPOINT_GROUP_EnpointGroupId
        FOREIGN KEY (EnpointGroupId) REFERENCES dbo.ENDPOINT_GROUP(Id) ON DELETE NO ACTION;
END;
ALTER TABLE dbo.SCHEDULE ALTER COLUMN StartDate nvarchar(max) NOT NULL;
ALTER TABLE dbo.SCHEDULE ALTER COLUMN EndDate nvarchar(max) NULL;
IF COL_LENGTH(N'dbo.ZONE', N'MaxSpeed') IS NOT NULL
BEGIN
    EXEC sp_rename N'dbo.ZONE.MaxSpeed', N'MaxSpeed     ', N'COLUMN';
    ALTER TABLE dbo.ZONE ALTER COLUMN [MaxSpeed     ] nvarchar(max) NOT NULL;
END;
""");
    }
}
