using LiBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiBot.Infrastructure.Persistence.Migrations;

[Migration("20261118000000_AlignLogicalErdFields")]
[DbContext(typeof(LiBotDbContext))]
public partial class AlignLogicalErdFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
IF OBJECT_ID(N'dbo.JOB_EVENT', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.JOB_EVENT', N'JobId') IS NOT NULL
   AND COL_LENGTH(N'dbo.JOB_EVENT', N'EventType') IS NOT NULL
BEGIN
    IF EXISTS
    (
        SELECT 1
        FROM dbo.JOB_EVENT AS e
        WHERE NOT EXISTS
        (
            SELECT 1
            FROM dbo.JOB_STEP AS js
            JOIN dbo.JOB_TASK AS jt ON jt.Id = js.JobTaskId
            WHERE jt.JobId = e.JobId
        )
    )
        THROW 51040, 'A persisted job event cannot be associated with a job step.', 1;

    DECLARE @DropJobEventForeignKeys nvarchar(max) = N'';
    SELECT @DropJobEventForeignKeys = STRING_AGG(
        N'ALTER TABLE dbo.JOB_EVENT DROP CONSTRAINT ' + QUOTENAME(fk.name) + N';', CHAR(10))
    FROM sys.foreign_keys AS fk
    WHERE fk.parent_object_id = OBJECT_ID(N'dbo.JOB_EVENT');
    IF @DropJobEventForeignKeys <> N'' EXEC sys.sp_executesql @DropJobEventForeignKeys;

    UPDATE e
       SET e.JobId = selectedStep.Id,
           e.Data = (SELECT e.EventType AS [event_type], e.Data AS [data]
                     FOR JSON PATH, WITHOUT_ARRAY_WRAPPER)
    FROM dbo.JOB_EVENT AS e
    CROSS APPLY
    (
        SELECT TOP (1) js.Id
        FROM dbo.JOB_STEP AS js
        JOIN dbo.JOB_TASK AS jt ON jt.Id = js.JobTaskId
        WHERE jt.JobId = e.JobId
        ORDER BY js.Sequence, js.Id
    ) AS selectedStep;

    EXEC sys.sp_rename N'dbo.JOB_EVENT.JobId', N'job_step_id', N'COLUMN';
    ALTER TABLE dbo.JOB_EVENT DROP COLUMN EventType;
END;
""");

        migrationBuilder.Sql("""
IF OBJECT_ID(N'dbo.JOB_TASK', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.JOB_TASK', N'Description') IS NOT NULL
   AND COL_LENGTH(N'dbo.JOB_TASK', N'StartedAt') IS NOT NULL
   AND COL_LENGTH(N'dbo.JOB_TASK', N'CompletedAt') IS NOT NULL
BEGIN
    IF EXISTS
    (
        SELECT 1
        FROM dbo.JOB_TASK AS jt
        WHERE NOT EXISTS (SELECT 1 FROM dbo.JOB_STEP AS js WHERE js.JobTaskId = jt.Id)
    )
        THROW 51041, 'A job task with persisted execution state has no job step.', 1;

    INSERT INTO dbo.JOB_EVENT (Id, job_step_id, EventTime, Data)
    SELECT NEWID(),
           selectedStep.Id,
           SYSUTCDATETIME(),
           (
               SELECT N'__JOB_TASK_STATE' AS [event_type],
                      JSON_QUERY((
                          SELECT
                              CASE WHEN ISJSON(jt.Description) = 1
                                         AND JSON_VALUE(jt.Description, '$.runtimeId') IS NOT NULL
                                   THEN JSON_VALUE(jt.Description, '$.description')
                                   ELSE jt.Description END AS [description],
                              COALESCE(JSON_VALUE(jt.Description, '$.runtimeId'), N'real') AS [runtimeId],
                              JSON_VALUE(jt.Description, '$.executionId') AS [executionId],
                              JSON_VALUE(jt.Description, '$.executionState') AS [executionState],
                              jt.StartedAt AS [startedAtUtc],
                              jt.CompletedAt AS [completedAtUtc]
                          FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
                      )) AS [data]
               FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
           )
    FROM dbo.JOB_TASK AS jt
    CROSS APPLY
    (
        SELECT TOP (1) js.Id
        FROM dbo.JOB_STEP AS js
        WHERE js.JobTaskId = jt.Id
        ORDER BY js.Sequence, js.Id
    ) AS selectedStep;
END;
""");

        migrationBuilder.Sql("""
-- Retain receipt/return history in BOOK_ISSUE, which is the ERD transaction table.
IF OBJECT_ID(N'dbo.BOOK_RECEIPT', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.BOOK_RECEIPT_DETAIL', N'U') IS NOT NULL
BEGIN
    DECLARE @ReceiptMap TABLE ([OldId] uniqueidentifier PRIMARY KEY, [NewId] uniqueidentifier NOT NULL);
    INSERT INTO @ReceiptMap ([OldId], [NewId])
    SELECT Id, NEWID() FROM dbo.BOOK_RECEIPT;

    DECLARE @DropReceiptForeignKeys nvarchar(max) = N'';
    SELECT @DropReceiptForeignKeys = STRING_AGG(
        N'ALTER TABLE dbo.BOOK_RECEIPT_DETAIL DROP CONSTRAINT ' + QUOTENAME(fk.name) + N';', CHAR(10))
    FROM sys.foreign_keys AS fk
    WHERE fk.parent_object_id = OBJECT_ID(N'dbo.BOOK_RECEIPT_DETAIL');
    IF @DropReceiptForeignKeys <> N'' EXEC sys.sp_executesql @DropReceiptForeignKeys;

    INSERT INTO dbo.BOOK_ISSUE (Id, IssueType, IssueDate, Status, Total, Note)
    SELECT map.NewId, receipt.ReceiptType, receipt.ReceivedDate, receipt.Status, receipt.Total, receipt.Note
    FROM dbo.BOOK_RECEIPT AS receipt
    JOIN @ReceiptMap AS map ON map.OldId = receipt.Id;

    INSERT INTO dbo.BOOK_ISSUE_DETAIL (Id, IssueId, BookItemId, BookId, Note)
    SELECT NEWID(), map.NewId, detail.BookItemId, detail.BookId, detail.Note
    FROM dbo.BOOK_RECEIPT_DETAIL AS detail
    JOIN @ReceiptMap AS map ON map.OldId = detail.ReceiptId;
END;

-- Remove the issue-detail relationships that depend on the fields absent from the ERD.
DECLARE @DropIssueForeignKeys nvarchar(max) = N'';
SELECT @DropIssueForeignKeys = STRING_AGG(
    N'ALTER TABLE dbo.BOOK_ISSUE_DETAIL DROP CONSTRAINT ' + QUOTENAME(fk.name) + N';', CHAR(10))
FROM sys.foreign_keys AS fk
WHERE fk.parent_object_id = OBJECT_ID(N'dbo.BOOK_ISSUE_DETAIL');
IF @DropIssueForeignKeys <> N'' EXEC sys.sp_executesql @DropIssueForeignKeys;

DECLARE @DropIssueBookIndexes nvarchar(max) = N'';
SELECT @DropIssueBookIndexes = STRING_AGG(
    N'DROP INDEX ' + QUOTENAME(i.name) + N' ON dbo.BOOK_ISSUE_DETAIL;', CHAR(10))
FROM sys.indexes AS i
JOIN sys.index_columns AS ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
JOIN sys.columns AS c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
WHERE i.object_id = OBJECT_ID(N'dbo.BOOK_ISSUE_DETAIL')
  AND i.name IS NOT NULL AND i.is_primary_key = 0 AND i.is_unique_constraint = 0
  AND c.name = N'BookId';
IF @DropIssueBookIndexes <> N'' EXEC sys.sp_executesql @DropIssueBookIndexes;

DECLARE @DropIssueUniqueConstraints nvarchar(max) = N'';
SELECT @DropIssueUniqueConstraints = STRING_AGG(
    N'ALTER TABLE dbo.BOOK_ISSUE_DETAIL DROP CONSTRAINT ' + QUOTENAME(k.name) + N';', CHAR(10))
FROM sys.key_constraints AS k
WHERE k.parent_object_id = OBJECT_ID(N'dbo.BOOK_ISSUE_DETAIL') AND k.type = N'UQ';
IF @DropIssueUniqueConstraints <> N'' EXEC sys.sp_executesql @DropIssueUniqueConstraints;

IF COL_LENGTH(N'dbo.BOOK_ISSUE_DETAIL', N'BookId') IS NOT NULL
    ALTER TABLE dbo.BOOK_ISSUE_DETAIL DROP COLUMN BookId;

IF OBJECT_ID(N'dbo.BOOK_RECEIPT_DETAIL', N'U') IS NOT NULL
    DROP TABLE dbo.BOOK_RECEIPT_DETAIL;
IF OBJECT_ID(N'dbo.BOOK_RECEIPT', N'U') IS NOT NULL
    DROP TABLE dbo.BOOK_RECEIPT;

DECLARE @DropBookItemAlternateKeys nvarchar(max) = N'';
SELECT @DropBookItemAlternateKeys = STRING_AGG(
    N'ALTER TABLE dbo.BOOK_ITEM DROP CONSTRAINT ' + QUOTENAME(k.name) + N';', CHAR(10))
FROM sys.key_constraints AS k
JOIN sys.index_columns AS ic ON ic.object_id = k.parent_object_id AND ic.index_id = k.unique_index_id
JOIN sys.columns AS c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
WHERE k.parent_object_id = OBJECT_ID(N'dbo.BOOK_ITEM') AND k.type = N'UQ' AND c.name = N'BookId';
IF @DropBookItemAlternateKeys <> N'' EXEC sys.sp_executesql @DropBookItemAlternateKeys;

IF COL_LENGTH(N'dbo.JOB_TASK', N'Description') IS NOT NULL
    ALTER TABLE dbo.JOB_TASK DROP COLUMN Description;
IF COL_LENGTH(N'dbo.JOB_TASK', N'StartedAt') IS NOT NULL
    ALTER TABLE dbo.JOB_TASK DROP COLUMN StartedAt;
IF COL_LENGTH(N'dbo.JOB_TASK', N'CompletedAt') IS NOT NULL
    ALTER TABLE dbo.JOB_TASK DROP COLUMN CompletedAt;
""");

        migrationBuilder.Sql($$"""
DECLARE @TargetFields TABLE ([TableName] sysname NOT NULL, [ColumnName] sysname NOT NULL);
INSERT INTO @TargetFields ([TableName], [ColumnName]) VALUES
{{MigrationSql.GetLogicalErdFieldList()}};

DECLARE @TableName sysname;
DECLARE @ColumnName sysname;
DECLARE @ObjectId int;
DECLARE @OldColumnName sysname;
DECLARE @RenameCursor CURSOR;
SET @RenameCursor = CURSOR LOCAL FAST_FORWARD FOR
    SELECT [TableName], [ColumnName] FROM @TargetFields ORDER BY [TableName], [ColumnName];
OPEN @RenameCursor;
FETCH NEXT FROM @RenameCursor INTO @TableName, @ColumnName;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @ObjectId = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName), N'U');
    SET @OldColumnName = NULL;
    IF @ObjectId IS NOT NULL AND COL_LENGTH(N'dbo.' + @TableName, @ColumnName) IS NULL
    BEGIN
        SELECT TOP (1) @OldColumnName = c.name
        FROM sys.columns AS c
        WHERE c.object_id = @ObjectId
          AND LOWER(REPLACE(c.name, N'_', N'')) = LOWER(REPLACE(@ColumnName, N'_', N''))
        ORDER BY c.column_id;

        IF @OldColumnName IS NOT NULL
        BEGIN
            DECLARE @RenameStatement nvarchar(max) =
                N'EXEC sys.sp_rename N''' + QUOTENAME(OBJECT_SCHEMA_NAME(@ObjectId)) + N'.'
                + QUOTENAME(OBJECT_NAME(@ObjectId)) + N'.' + QUOTENAME(@OldColumnName)
                + N''', N''' + REPLACE(@ColumnName, N'''', N'''''') + N''', N''COLUMN'';';
            EXEC sys.sp_executesql @RenameStatement;
        END;
    END;
    FETCH NEXT FROM @RenameCursor INTO @TableName, @ColumnName;
END;
CLOSE @RenameCursor;
DEALLOCATE @RenameCursor;
""");

        migrationBuilder.Sql("""
IF COL_LENGTH(N'dbo.ENDPOINT_GROUP_MEMBER', N'enpoint_group_id') IS NULL
   AND COL_LENGTH(N'dbo.ENDPOINT_GROUP_MEMBER', N'EndpointGroupId') IS NOT NULL
    EXEC sys.sp_rename N'dbo.ENDPOINT_GROUP_MEMBER.EndpointGroupId', N'enpoint_group_id', N'COLUMN';
IF COL_LENGTH(N'dbo.INVENTORY_REPORT', N'total_shelfs') IS NULL
   AND COL_LENGTH(N'dbo.INVENTORY_REPORT', N'TotalShelves') IS NOT NULL
    EXEC sys.sp_rename N'dbo.INVENTORY_REPORT.TotalShelves', N'total_shelfs', N'COLUMN';
IF COL_LENGTH(N'dbo.INVENTORY_REPORT', N'total_book_scaned') IS NULL
   AND COL_LENGTH(N'dbo.INVENTORY_REPORT', N'TotalBooksScanned') IS NOT NULL
    EXEC sys.sp_rename N'dbo.INVENTORY_REPORT.TotalBooksScanned', N'total_book_scaned', N'COLUMN';
IF COL_LENGTH(N'dbo.JOB_STEP', N'configuration') IS NULL
   AND COL_LENGTH(N'dbo.JOB_STEP', N'Description') IS NOT NULL
    EXEC sys.sp_rename N'dbo.JOB_STEP.Description', N'configuration', N'COLUMN';

IF COL_LENGTH(N'dbo.JOB_TASK', N'sequence_no') IS NULL
BEGIN
    ALTER TABLE dbo.JOB_TASK ADD [sequence_no] int NULL;
    EXEC sys.sp_executesql N'
        ;WITH SequencedTasks AS
        (
            SELECT Id, ROW_NUMBER() OVER (PARTITION BY job_id ORDER BY Id) AS SequenceNo
            FROM dbo.JOB_TASK
        )
        UPDATE task SET sequence_no = SequencedTasks.SequenceNo
        FROM dbo.JOB_TASK AS task
        JOIN SequencedTasks ON SequencedTasks.Id = task.Id;';
    ALTER TABLE dbo.JOB_TASK ALTER COLUMN [sequence_no] int NOT NULL;
END;

IF COL_LENGTH(N'dbo.TASK_STEP', N'description') IS NULL
    ALTER TABLE dbo.TASK_STEP ADD [description] nvarchar(max) NULL;
""");

        migrationBuilder.Sql("""
IF OBJECT_ID(N'dbo.CONFIG_BATTERY_ROBOT', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CONFIG_BATTERY_ROBOT
    (
        [id] uniqueidentifier NOT NULL PRIMARY KEY,
        [account_id] uniqueidentifier NOT NULL,
        [robot_id] uniqueidentifier NOT NULL,
        [battery_threshold] float NOT NULL,
        [is_active] bit NOT NULL CONSTRAINT [df_config_battery_robot_is_active] DEFAULT (1)
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.JOB_TASK') AND name = N'uq_job_task_sequence')
    ALTER TABLE dbo.JOB_TASK ADD CONSTRAINT [uq_job_task_sequence] UNIQUE ([job_id], [sequence_no]);

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID(N'dbo.JOB_EVENT') AND name = N'fk_job_event_job_step_id')
    ALTER TABLE dbo.JOB_EVENT ADD CONSTRAINT [fk_job_event_job_step_id]
        FOREIGN KEY ([job_step_id]) REFERENCES dbo.JOB_STEP([Id]) ON UPDATE CASCADE ON DELETE NO ACTION;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.JOB_EVENT') AND name = N'ix_job_event_job_step_id')
    CREATE INDEX [ix_job_event_job_step_id] ON dbo.JOB_EVENT([job_step_id]);

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID(N'dbo.BOOK_ISSUE_DETAIL') AND name = N'fk_book_issue_detail_issue_id')
    ALTER TABLE dbo.BOOK_ISSUE_DETAIL ADD CONSTRAINT [fk_book_issue_detail_issue_id]
        FOREIGN KEY ([issue_id]) REFERENCES dbo.BOOK_ISSUE([Id]) ON UPDATE CASCADE ON DELETE NO ACTION;
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID(N'dbo.BOOK_ISSUE_DETAIL') AND name = N'fk_book_issue_detail_book_item_id')
    ALTER TABLE dbo.BOOK_ISSUE_DETAIL ADD CONSTRAINT [fk_book_issue_detail_book_item_id]
        FOREIGN KEY ([book_item_id]) REFERENCES dbo.BOOK_ITEM([Id]) ON UPDATE CASCADE ON DELETE NO ACTION;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID(N'dbo.CONFIG_BATTERY_ROBOT') AND name = N'fk_config_battery_robot_account_id')
    ALTER TABLE dbo.CONFIG_BATTERY_ROBOT ADD CONSTRAINT [fk_config_battery_robot_account_id]
        FOREIGN KEY ([account_id]) REFERENCES dbo.ACCOUNT([Id]) ON UPDATE CASCADE ON DELETE NO ACTION;
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID(N'dbo.CONFIG_BATTERY_ROBOT') AND name = N'fk_config_battery_robot_robot_id')
    ALTER TABLE dbo.CONFIG_BATTERY_ROBOT ADD CONSTRAINT [fk_config_battery_robot_robot_id]
        FOREIGN KEY ([robot_id]) REFERENCES dbo.ROBOT([Id]) ON UPDATE CASCADE ON DELETE NO ACTION;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.CONFIG_BATTERY_ROBOT') AND name = N'ix_config_battery_robot_account_id')
    CREATE INDEX [ix_config_battery_robot_account_id] ON dbo.CONFIG_BATTERY_ROBOT([account_id]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.CONFIG_BATTERY_ROBOT') AND name = N'ix_config_battery_robot_robot_id')
    CREATE INDEX [ix_config_battery_robot_robot_id] ON dbo.CONFIG_BATTERY_ROBOT([robot_id]);
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql("THROW 51042, 'Restore the database backup to reverse this field alignment.', 1;");
}
