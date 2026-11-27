using LiBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiBot.Infrastructure.Persistence.Migrations;

[Migration("20261117000000_AlignErdNullabilityAndScheduleTimestamp")]
[DbContext(typeof(LiBotDbContext))]
public partial class AlignErdNullabilityAndScheduleTimestamp : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
DECLARE @DropForeignKeys nvarchar(max) = N'';
SELECT @DropForeignKeys = STRING_AGG(
    N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(parent_object_id)) + N'.'
        + QUOTENAME(OBJECT_NAME(parent_object_id)) + N' DROP CONSTRAINT ' + QUOTENAME(name) + N';',
    CHAR(10))
FROM
(
    SELECT DISTINCT fk.name, fk.parent_object_id
    FROM sys.foreign_keys AS fk
    JOIN sys.foreign_key_columns AS fkc ON fkc.constraint_object_id = fk.object_id
    JOIN sys.columns AS c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
    WHERE (fk.parent_object_id = OBJECT_ID(N'dbo.JOB') AND c.name = N'WorkflowId')
       OR (fk.parent_object_id = OBJECT_ID(N'dbo.JOB_TASK') AND c.name = N'WorkflowTaskId')
) AS existing;
IF @DropForeignKeys <> N'' EXEC sys.sp_executesql @DropForeignKeys;

DECLARE @DropIndexes nvarchar(max) = N'';
SELECT @DropIndexes = STRING_AGG(
    N'DROP INDEX ' + QUOTENAME(name) + N' ON ' + QUOTENAME(OBJECT_SCHEMA_NAME(object_id))
        + N'.' + QUOTENAME(OBJECT_NAME(object_id)) + N';',
    CHAR(10))
FROM
(
    SELECT DISTINCT i.name, i.object_id
    FROM sys.indexes AS i
    JOIN sys.index_columns AS ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
    JOIN sys.columns AS c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
    WHERE i.name IS NOT NULL AND i.is_primary_key = 0 AND i.is_unique_constraint = 0
      AND ic.key_ordinal > 0
      AND ((i.object_id = OBJECT_ID(N'dbo.JOB') AND c.name = N'WorkflowId')
        OR (i.object_id = OBJECT_ID(N'dbo.JOB_TASK') AND c.name = N'WorkflowTaskId'))
) AS existing;
IF @DropIndexes <> N'' EXEC sys.sp_executesql @DropIndexes;
""");

        migrationBuilder.Sql("""
IF EXISTS (SELECT 1 FROM dbo.[JOB] WHERE [WorkflowId] IS NULL)
    THROW 51027, 'JOB.WorkflowId contains NULL values and must be mapped before migration.', 1;
IF EXISTS (SELECT 1 FROM dbo.[JOB_TASK] WHERE [WorkflowTaskId] IS NULL)
    THROW 51028, 'JOB_TASK.WorkflowTaskId contains NULL values and must be mapped before migration.', 1;
UPDATE dbo.[TASK_STEP] SET [Configuration] = N'' WHERE [Configuration] IS NULL;
""");
        migrationBuilder.Sql("ALTER TABLE dbo.[JOB] ALTER COLUMN [WorkflowId] uniqueidentifier NOT NULL;");
        migrationBuilder.Sql("ALTER TABLE dbo.[JOB_TASK] ALTER COLUMN [WorkflowTaskId] uniqueidentifier NOT NULL;");
        migrationBuilder.Sql("ALTER TABLE dbo.[INVENTORY_REPORT] ALTER COLUMN [Data] nvarchar(max) NULL;");
        migrationBuilder.Sql("ALTER TABLE dbo.[PATROL_REPORT] ALTER COLUMN [Data] nvarchar(max) NULL;");
        migrationBuilder.Sql("ALTER TABLE dbo.[TASK_STEP] ALTER COLUMN [Configuration] nvarchar(max) NOT NULL;");
        migrationBuilder.Sql("ALTER TABLE dbo.[WORKFLOW] ALTER COLUMN [Description] nvarchar(max) NULL;");
        migrationBuilder.Sql("CREATE INDEX [IX_JOB_WorkflowId] ON dbo.[JOB] ([WorkflowId]);");
        migrationBuilder.Sql("CREATE INDEX [IX_JOB_TASK_WorkflowTaskId] ON dbo.[JOB_TASK] ([WorkflowTaskId]);");
        migrationBuilder.Sql("ALTER TABLE dbo.[JOB] ADD CONSTRAINT [FK_JOB_WORKFLOW_WorkflowId] FOREIGN KEY ([WorkflowId]) REFERENCES dbo.[WORKFLOW]([Id]) ON DELETE NO ACTION;");
        migrationBuilder.Sql("ALTER TABLE dbo.[JOB_TASK] ADD CONSTRAINT [FK_JOB_TASK_WORKFLOW_TASK_WorkflowTaskId] FOREIGN KEY ([WorkflowTaskId]) REFERENCES dbo.[WORKFLOW_TASK]([Id]) ON DELETE NO ACTION;");
        migrationBuilder.Sql("""
IF COL_LENGTH(N'dbo.SCHEDULE', N'UpdatedAt') IS NULL
    EXEC sys.sp_executesql N'ALTER TABLE dbo.SCHEDULE ADD UpdatedAt datetime2 NULL';
IF COL_LENGTH(N'dbo.SCHEDULE', N'UpdatedAt') IS NOT NULL
BEGIN
    EXEC sys.sp_executesql N'UPDATE dbo.SCHEDULE SET UpdatedAt = COALESCE(UpdatedAt, CreatedAt, SYSUTCDATETIME())';
    EXEC sys.sp_executesql N'ALTER TABLE dbo.SCHEDULE ALTER COLUMN UpdatedAt datetime2 NOT NULL';
END;
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql("THROW 51026, 'Restore the pre-migration backup to reverse this schema alignment.', 1;");
}
