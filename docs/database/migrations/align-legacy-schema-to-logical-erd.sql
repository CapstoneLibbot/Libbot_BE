SET XACT_ABORT ON;

IF EXISTS (SELECT 1 FROM dbo.CONFIG_LOG)
    THROW 51021, 'CONFIG_LOG contains data and must be archived before the ERD migration can remove this legacy table.', 1;

DECLARE @DropForeignKeys nvarchar(max) = N'';
SELECT @DropForeignKeys = STRING_AGG(
    N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(fk.parent_object_id)) + N'.'
        + QUOTENAME(OBJECT_NAME(fk.parent_object_id)) + N' DROP CONSTRAINT ' + QUOTENAME(fk.name) + N';',
    CHAR(10))
FROM sys.foreign_keys AS fk
WHERE OBJECT_SCHEMA_NAME(fk.parent_object_id) = N'dbo';
IF @DropForeignKeys <> N'' EXEC sys.sp_executesql @DropForeignKeys;
GO

DROP INDEX IF EXISTS [IX_ACCOUNT_Email] ON dbo.[ACCOUNT];
DROP INDEX IF EXISTS [IX_ACCOUNT_Username] ON dbo.[ACCOUNT];
DROP INDEX IF EXISTS [IX_BOOK_Isbn] ON dbo.[BOOK];
DROP INDEX IF EXISTS [IX_BOOK_ITEM_Barcode] ON dbo.[BOOK_ITEM];
DROP INDEX IF EXISTS [IX_ROBOT_SerialNumber] ON dbo.[ROBOT];
DROP INDEX IF EXISTS [IX_ENDPOINT_GROUP_MEMBER_EndpointGroupId_EndpointId] ON dbo.[ENDPOINT_GROUP_MEMBER];
DROP INDEX IF EXISTS [IX_JOB_CreatedBy] ON dbo.[JOB];
DROP INDEX IF EXISTS [IX_JOB_RobotId] ON dbo.[JOB];
DROP INDEX IF EXISTS [IX_JOB_WorkflowId] ON dbo.[JOB];
DROP INDEX IF EXISTS [IX_JOB_TASK_WorkflowTaskId] ON dbo.[JOB_TASK];
DROP INDEX IF EXISTS [IX_SCHEDULE_CreatedBy] ON dbo.[SCHEDULE];
DROP INDEX IF EXISTS [IX_SHELF_EndpointId] ON dbo.[SHELF];
DROP INDEX IF EXISTS [IX_MAP_FloorId] ON dbo.[MAP];
DROP INDEX IF EXISTS [IX_INVENTORY_REPORT_JobId] ON dbo.[INVENTORY_REPORT];
DROP INDEX IF EXISTS [IX_PATROL_REPORT_JobId] ON dbo.[PATROL_REPORT];
DROP INDEX IF EXISTS [IX_AREA_EndpointId] ON dbo.[AREA];
DROP INDEX IF EXISTS [IX_AREA_FloorId] ON dbo.[AREA];
DROP INDEX IF EXISTS [IX_CONFIG_LOG_AccountId] ON dbo.[CONFIG_LOG];
DROP INDEX IF EXISTS [IX_CONFIG_LOG_RobotId] ON dbo.[CONFIG_LOG];

DECLARE @DropLegacyDefaults nvarchar(max) = N'';
SELECT @DropLegacyDefaults = STRING_AGG(
    N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(dc.parent_object_id)) + N'.'
        + QUOTENAME(OBJECT_NAME(dc.parent_object_id)) + N' DROP CONSTRAINT ' + QUOTENAME(dc.name) + N';',
    CHAR(10))
FROM sys.default_constraints AS dc
JOIN sys.columns AS c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
JOIN sys.tables AS t ON t.object_id = dc.parent_object_id
WHERE (t.name = N'ZONE' AND c.name = N'RotationAngle')
   OR (t.name = N'JOB_TASK' AND c.name = N'RuntimeId')
   OR (t.name = N'ROBOT_LOG' AND c.name = N'Yaw')
   OR (t.name = N'WORKFLOW' AND c.name = N'IsActive');
IF @DropLegacyDefaults <> N'' EXEC sys.sp_executesql @DropLegacyDefaults;

DECLARE @DropExtraAuditDefaults nvarchar(max) = N'';
SELECT @DropExtraAuditDefaults = STRING_AGG(
    N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(dc.parent_object_id)) + N'.'
        + QUOTENAME(OBJECT_NAME(dc.parent_object_id)) + N' DROP CONSTRAINT ' + QUOTENAME(dc.name) + N';',
    CHAR(10))
FROM sys.default_constraints AS dc
JOIN sys.columns AS c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
JOIN sys.tables AS t ON t.object_id = dc.parent_object_id
WHERE t.name IN (N'BOOK_CATEGORY', N'ENDPOINT', N'MAP')
  AND c.name IN (N'CreatedAtUtc', N'UpdatedAtUtc');
IF @DropExtraAuditDefaults <> N'' EXEC sys.sp_executesql @DropExtraAuditDefaults;

DECLARE @DropExtraAuditColumns nvarchar(max) = N'';
SELECT @DropExtraAuditColumns = STRING_AGG(
    N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(c.object_id)) + N'.'
        + QUOTENAME(OBJECT_NAME(c.object_id)) + N' DROP COLUMN ' + QUOTENAME(c.name) + N';',
    CHAR(10))
FROM sys.columns AS c
JOIN sys.tables AS t ON t.object_id = c.object_id
WHERE t.name IN (N'BOOK_CATEGORY', N'ENDPOINT', N'MAP')
  AND c.name IN (N'CreatedAtUtc', N'UpdatedAtUtc');
IF @DropExtraAuditColumns <> N'' EXEC sys.sp_executesql @DropExtraAuditColumns;
GO

IF EXISTS (SELECT 1 FROM dbo.[AREA] AS a JOIN dbo.[ENDPOINT_GROUP] AS eg ON eg.Id = a.Id)
    THROW 51022, 'An AREA id already exists in ENDPOINT_GROUP; resolve the id collision before migration.', 1;

UPDATE dbo.[MAP]
SET [FloorId] = 'C556898A-4671-4DB9-ADCD-5D7BE26AEFF1'
WHERE [FloorId] IS NULL;
ALTER TABLE dbo.[MAP] ALTER COLUMN [FloorId] uniqueidentifier NOT NULL;

ALTER TABLE dbo.[ACCOUNT] ALTER COLUMN [Status] nvarchar(max) NOT NULL;
EXEC sys.sp_rename N'dbo.ACCOUNT.CreatedAtUtc', N'CreatedAt', N'COLUMN';
EXEC sys.sp_rename N'dbo.ACCOUNT.LastLoginAtUtc', N'LastLoginAt', N'COLUMN';
GO
ALTER TABLE dbo.[ACCOUNT] ADD [UpdatedAt] datetime2 NULL;
GO
UPDATE dbo.[ACCOUNT] SET [UpdatedAt] = [CreatedAt];
ALTER TABLE dbo.[ACCOUNT] ALTER COLUMN [UpdatedAt] datetime2 NOT NULL;

EXEC sys.sp_rename N'dbo.AUDIT_LOG.CreatedAtUtc', N'CreatedAt', N'COLUMN';
ALTER TABLE dbo.[AUDIT_LOG] DROP COLUMN [UpdatedAtUtc];
EXEC sys.sp_rename N'dbo.NOTIFICATION.CreatedAtUtc', N'CreatedAt', N'COLUMN';
EXEC sys.sp_rename N'dbo.NOTIFICATION.ReadAtUtc', N'ReadAt', N'COLUMN';
ALTER TABLE dbo.[NOTIFICATION] DROP COLUMN [UpdatedAtUtc];

EXEC sys.sp_rename N'dbo.BOOK.Image', N'ImageUrl', N'COLUMN';
ALTER TABLE dbo.[BOOK] DROP COLUMN [CreatedAtUtc], [UpdatedAtUtc];
EXEC sys.sp_rename N'dbo.BOOK_ITEM.LastInventoryAtUtc', N'LastInventoryAt', N'COLUMN';
ALTER TABLE dbo.[BOOK_ITEM] DROP COLUMN [CreatedAtUtc], [UpdatedAtUtc];

ALTER TABLE dbo.[FLOOR] ADD [Status] nvarchar(max) NULL;
GO
UPDATE dbo.[FLOOR] SET [Status] = N'Active';
ALTER TABLE dbo.[FLOOR] ALTER COLUMN [Status] nvarchar(max) NOT NULL;
ALTER TABLE dbo.[FLOOR] DROP COLUMN [CreatedAtUtc], [UpdatedAtUtc];

ALTER TABLE dbo.[AISLE] ADD [Status] nvarchar(max) NULL;
GO
UPDATE dbo.[AISLE] SET [Status] = N'Active';
ALTER TABLE dbo.[AISLE] ALTER COLUMN [Status] nvarchar(max) NOT NULL;
UPDATE dbo.[AISLE]
SET [Description] = COALESCE(NULLIF([Description], N''), N'Legacy code: ' + [Code])
WHERE [Code] IS NOT NULL;
ALTER TABLE dbo.[AISLE] DROP COLUMN [Code], [CreatedAtUtc], [UpdatedAtUtc];

ALTER TABLE dbo.[SHELF] ADD [Description] nvarchar(max) NULL;
ALTER TABLE dbo.[SHELF] ADD [Status] nvarchar(max) NULL;
ALTER TABLE dbo.[SHELF] ADD [StartEndpointId] uniqueidentifier NULL;
ALTER TABLE dbo.[SHELF] ADD [EndEndpointId] uniqueidentifier NULL;
GO
UPDATE dbo.[SHELF]
SET [Description] = COALESCE(NULLIF([Description], N''), N'Legacy code: ' + [Code]),
    [Status] = N'Active',
    [StartEndpointId] = [EndpointId],
    [EndEndpointId] = [EndpointId];
ALTER TABLE dbo.[SHELF] ALTER COLUMN [Status] nvarchar(max) NOT NULL;
ALTER TABLE dbo.[SHELF] ALTER COLUMN [StartEndpointId] uniqueidentifier NOT NULL;
ALTER TABLE dbo.[SHELF] ALTER COLUMN [EndEndpointId] uniqueidentifier NOT NULL;
ALTER TABLE dbo.[SHELF] DROP COLUMN [EndpointId], [Code], [CreatedAtUtc], [UpdatedAtUtc];

ALTER TABLE dbo.[SHELF_LEVEL] ADD [Name] nvarchar(max) NULL;
ALTER TABLE dbo.[SHELF_LEVEL] ADD [Status] nvarchar(max) NULL;
GO
UPDATE dbo.[SHELF_LEVEL]
SET [Name] = N'Level ' + CONVERT(nvarchar(20), [LevelNumber]), [Status] = N'Active';
ALTER TABLE dbo.[SHELF_LEVEL] ALTER COLUMN [Name] nvarchar(max) NOT NULL;
ALTER TABLE dbo.[SHELF_LEVEL] ALTER COLUMN [Status] nvarchar(max) NOT NULL;
ALTER TABLE dbo.[SHELF_LEVEL] DROP COLUMN [CreatedAtUtc], [UpdatedAtUtc];

ALTER TABLE dbo.[ENDPOINT_GROUP] ADD [Description] nvarchar(max) NULL;
GO
INSERT INTO dbo.[ENDPOINT_GROUP] ([Id], [Name], [Description], [CreatedAtUtc], [UpdatedAtUtc])
SELECT [Id], [Name], [Description], SYSUTCDATETIME(), SYSUTCDATETIME()
FROM dbo.[AREA];
INSERT INTO dbo.[ENDPOINT_GROUP_MEMBER] ([Id], [EndpointGroupId], [EndpointId], [CreatedAtUtc], [UpdatedAtUtc])
SELECT NEWID(), [Id], [EndpointId], SYSUTCDATETIME(), SYSUTCDATETIME()
FROM dbo.[AREA]
WHERE [EndpointId] IS NOT NULL;
ALTER TABLE dbo.[ENDPOINT_GROUP] DROP COLUMN [CreatedAtUtc], [UpdatedAtUtc];
ALTER TABLE dbo.[ENDPOINT_GROUP_MEMBER] DROP COLUMN [CreatedAtUtc], [UpdatedAtUtc];
DROP TABLE dbo.[AREA];
GO

EXEC sys.sp_rename N'dbo.ZONE.CreatedAtUtc', N'CreatedAt', N'COLUMN';
ALTER TABLE dbo.[ZONE] DROP COLUMN [UpdatedAtUtc], [Width], [Height], [RotationAngle];

EXEC sys.sp_rename N'dbo.JOB_EVENT.EventTimeUtc', N'EventTime', N'COLUMN';
ALTER TABLE dbo.[JOB_EVENT] DROP COLUMN [CreatedAtUtc], [UpdatedAtUtc];
ALTER TABLE dbo.[FEEDBACK] DROP COLUMN [CreatedAtUtc], [UpdatedAtUtc];

ALTER TABLE dbo.[WORKFLOW] ADD [Status] nvarchar(max) NULL;
GO
UPDATE dbo.[WORKFLOW] SET [Status] = CASE WHEN [IsActive] = 1 THEN N'Active' ELSE N'Inactive' END;
ALTER TABLE dbo.[WORKFLOW] ALTER COLUMN [Status] nvarchar(max) NOT NULL;
ALTER TABLE dbo.[WORKFLOW] ALTER COLUMN [Description] nvarchar(max) NULL;
ALTER TABLE dbo.[WORKFLOW] ADD [CreateAt] datetime2 NULL;
GO
UPDATE dbo.[WORKFLOW] SET [CreateAt] = SYSUTCDATETIME();
ALTER TABLE dbo.[WORKFLOW] ALTER COLUMN [CreateAt] datetime2 NOT NULL;
ALTER TABLE dbo.[WORKFLOW] DROP COLUMN [IsActive];

EXEC sys.sp_rename N'dbo.WORKFLOW_TASK.SequenceNumber', N'SequenceNo', N'COLUMN';
EXEC sys.sp_rename N'dbo.WORKFLOW_TASK.CreatedAtUtc', N'CreatedAt', N'COLUMN';
ALTER TABLE dbo.[WORKFLOW_TASK] DROP COLUMN [UpdatedAtUtc];

ALTER TABLE dbo.[TASK_STEP] ADD [SequenceNo] int NULL;
GO
WITH OrderedSteps AS
(
    SELECT [Id], CONVERT(int, ROW_NUMBER() OVER
        (PARTITION BY [WorkflowTaskId] ORDER BY [CreatedAtUtc], [Id]) - 1) AS [SequenceNo]
    FROM dbo.[TASK_STEP]
)
UPDATE step SET [SequenceNo] = ordered.[SequenceNo]
FROM dbo.[TASK_STEP] AS step
JOIN OrderedSteps AS ordered ON ordered.[Id] = step.[Id];
ALTER TABLE dbo.[TASK_STEP] ALTER COLUMN [SequenceNo] int NOT NULL;
ALTER TABLE dbo.[TASK_STEP] ALTER COLUMN [Configuration] nvarchar(max) NOT NULL;
EXEC sys.sp_rename N'dbo.TASK_STEP.CreatedAtUtc', N'CreatedAt', N'COLUMN';
ALTER TABLE dbo.[TASK_STEP] DROP COLUMN [UpdatedAtUtc];

ALTER TABLE dbo.[JOB_TASK] ADD [Description] nvarchar(max) NULL;
GO
UPDATE dbo.[JOB_TASK]
SET [Description] = (
    SELECT [RuntimeId] AS [runtimeId], [ExecutionId] AS [executionId], [ExecutionState] AS [executionState]
    FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
);
EXEC sys.sp_rename N'dbo.JOB_TASK.StartedAtUtc', N'StartedAt', N'COLUMN';
EXEC sys.sp_rename N'dbo.JOB_TASK.CompletedAtUtc', N'CompletedAt', N'COLUMN';
ALTER TABLE dbo.[JOB_TASK] DROP COLUMN [RuntimeId], [ExecutionId], [ExecutionState], [CreatedAtUtc], [UpdatedAtUtc];
ALTER TABLE dbo.[JOB_TASK] ALTER COLUMN [WorkflowTaskId] uniqueidentifier NOT NULL;

ALTER TABLE dbo.[JOB_STEP] ADD [Description] nvarchar(max) NULL;
ALTER TABLE dbo.[JOB_STEP] DROP COLUMN [CreatedAtUtc], [UpdatedAtUtc];

ALTER TABLE dbo.[ROBOT] ADD [MapId] uniqueidentifier NULL;
ALTER TABLE dbo.[ROBOT] ADD [CreateAt] datetime2 NULL;
ALTER TABLE dbo.[ROBOT] ADD [UpdateAt] datetime2 NULL;
GO
UPDATE dbo.[ROBOT]
SET [MapId] = COALESCE([MapId], 'B4C285C3-4E43-42FF-BF90-FF2267A1BFEE'),
    [CreateAt] = COALESCE([CreateAt], SYSUTCDATETIME()),
    [UpdateAt] = COALESCE([UpdateAt], SYSUTCDATETIME());
ALTER TABLE dbo.[ROBOT] ALTER COLUMN [MapId] uniqueidentifier NOT NULL;
ALTER TABLE dbo.[ROBOT] ALTER COLUMN [CreateAt] datetime2 NOT NULL;
ALTER TABLE dbo.[ROBOT] ALTER COLUMN [UpdateAt] datetime2 NOT NULL;

IF EXISTS (SELECT 1 FROM dbo.[JOB] WHERE [RobotId] IS NULL)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.[ROBOT] WHERE [SerialNumber] = N'MIGRATED-REAL-RUNTIME')
    BEGIN
        INSERT INTO dbo.[ROBOT] ([Id], [MapId], [Name], [SerialNumber], [Status], [BatteryLevel], [CreateAt], [UpdateAt])
        VALUES ('A0D2FA77-FD7A-4B20-A2E4-934BACBDDF01', 'B4C285C3-4E43-42FF-BF90-FF2267A1BFEE',
            N'Legacy offline robot (unassigned)', N'MIGRATED-REAL-RUNTIME', N'Offline', 0, SYSUTCDATETIME(), SYSUTCDATETIME());
    END;
    UPDATE dbo.[JOB]
    SET [RobotId] = (SELECT TOP (1) [Id] FROM dbo.[ROBOT] WHERE [SerialNumber] = N'MIGRATED-REAL-RUNTIME')
    WHERE [RobotId] IS NULL;
END;
EXEC sys.sp_rename N'dbo.JOB.CreatedAtUtc', N'CreateAt', N'COLUMN';
EXEC sys.sp_rename N'dbo.JOB.StartedAtUtc', N'StartedAt', N'COLUMN';
EXEC sys.sp_rename N'dbo.JOB.CompletedAtUtc', N'CompletedAt', N'COLUMN';
ALTER TABLE dbo.[JOB] ALTER COLUMN [Description] nvarchar(max) NULL;
ALTER TABLE dbo.[JOB] ALTER COLUMN [RobotId] uniqueidentifier NOT NULL;
ALTER TABLE dbo.[JOB] ALTER COLUMN [WorkflowId] uniqueidentifier NOT NULL;
ALTER TABLE dbo.[JOB] DROP COLUMN [CreatedBy];

IF EXISTS (SELECT 1 FROM dbo.[SCHEDULE])
    THROW 51023, 'SCHEDULE contains rows that require an explicit WorkflowId mapping before migration.', 1;
ALTER TABLE dbo.[SCHEDULE] ADD [WorkflowId] uniqueidentifier NULL;
GO
ALTER TABLE dbo.[SCHEDULE] ALTER COLUMN [WorkflowId] uniqueidentifier NOT NULL;
ALTER TABLE dbo.[SCHEDULE] ADD [IsActive] bit NULL;
GO
UPDATE dbo.[SCHEDULE] SET [IsActive] = 1;
ALTER TABLE dbo.[SCHEDULE] ALTER COLUMN [IsActive] bit NOT NULL;
EXEC sys.sp_rename N'dbo.SCHEDULE.LastRunAtUtc', N'LastRunAt', N'COLUMN';
EXEC sys.sp_rename N'dbo.SCHEDULE.NextRunAtUtc', N'NextRunAt', N'COLUMN';
EXEC sys.sp_rename N'dbo.SCHEDULE.CreatedAtUtc', N'CreatedAt', N'COLUMN';
EXEC sys.sp_rename N'dbo.SCHEDULE.UpdatedAtUtc', N'UpdatedAt', N'COLUMN';
ALTER TABLE dbo.[SCHEDULE] DROP COLUMN [CreatedBy];

EXEC sys.sp_rename N'dbo.INVENTORY_REPORT.CreatedAtUtc', N'CreatedAt', N'COLUMN';
ALTER TABLE dbo.[INVENTORY_REPORT] DROP COLUMN [UpdatedAtUtc];
ALTER TABLE dbo.[INVENTORY_REPORT] ALTER COLUMN [JobId] uniqueidentifier NOT NULL;
ALTER TABLE dbo.[INVENTORY_REPORT] ALTER COLUMN [Data] nvarchar(max) NULL;

ALTER TABLE dbo.[PATROL_REPORT] ADD [StartedAt] datetime2 NULL;
ALTER TABLE dbo.[PATROL_REPORT] ADD [CompletedAt] datetime2 NULL;
EXEC sys.sp_rename N'dbo.PATROL_REPORT.DurationSeconds', N'DurationSec', N'COLUMN';
EXEC sys.sp_rename N'dbo.PATROL_REPORT.CreatedAtUtc', N'CreatedAt', N'COLUMN';
ALTER TABLE dbo.[PATROL_REPORT] DROP COLUMN [UpdatedAtUtc];
ALTER TABLE dbo.[PATROL_REPORT] ALTER COLUMN [JobId] uniqueidentifier NOT NULL;
ALTER TABLE dbo.[PATROL_REPORT] ALTER COLUMN [Data] nvarchar(max) NULL;

ALTER TABLE dbo.[ROBOT_LOG] ADD [MapId] uniqueidentifier NULL;
ALTER TABLE dbo.[ROBOT_LOG] ADD [Status] nvarchar(max) NULL;
ALTER TABLE dbo.[ROBOT_LOG] ADD [Data] nvarchar(max) NULL;
GO
UPDATE log
SET [MapId] = COALESCE(robot.[MapId], 'B4C285C3-4E43-42FF-BF90-FF2267A1BFEE'),
    [Status] = N'UNKNOWN',
    [Data] = COALESCE(log.[Data], (SELECT log.[Yaw] AS [yaw] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER))
FROM dbo.[ROBOT_LOG] AS log
JOIN dbo.[ROBOT] AS robot ON robot.[Id] = log.[RobotId];
UPDATE dbo.[ROBOT_LOG]
SET [MapId] = 'B4C285C3-4E43-42FF-BF90-FF2267A1BFEE', [Status] = N'UNKNOWN'
WHERE [MapId] IS NULL;
ALTER TABLE dbo.[ROBOT_LOG] ALTER COLUMN [MapId] uniqueidentifier NOT NULL;
ALTER TABLE dbo.[ROBOT_LOG] ALTER COLUMN [Status] nvarchar(max) NOT NULL;
EXEC sys.sp_rename N'dbo.ROBOT_LOG.RecordedAtUtc', N'RecordAt', N'COLUMN';
ALTER TABLE dbo.[ROBOT_LOG] DROP COLUMN [Yaw], [CreatedAtUtc], [UpdatedAtUtc];

ALTER TABLE dbo.[BOOK_ITEM] ADD CONSTRAINT [UQ_BOOK_ITEM_Id_BookId] UNIQUE ([Id], [BookId]);

CREATE INDEX [IX_MAP_FloorId] ON dbo.[MAP] ([FloorId]);
CREATE INDEX [IX_JOB_RobotId] ON dbo.[JOB] ([RobotId]);
CREATE INDEX [IX_JOB_WorkflowId] ON dbo.[JOB] ([WorkflowId]);
CREATE INDEX [IX_JOB_TASK_WorkflowTaskId] ON dbo.[JOB_TASK] ([WorkflowTaskId]);
CREATE INDEX [IX_INVENTORY_REPORT_JobId] ON dbo.[INVENTORY_REPORT] ([JobId]);
CREATE INDEX [IX_PATROL_REPORT_JobId] ON dbo.[PATROL_REPORT] ([JobId]);

IF EXISTS (SELECT 1 FROM dbo.[CONFIG_LOG])
    THROW 51024, 'CONFIG_LOG changed during migration; archive this data before removal.', 1;
DROP TABLE dbo.[CONFIG_LOG];

ALTER TABLE dbo.[AUDIT_LOG] ADD CONSTRAINT [FK_AUDIT_LOG_ACCOUNT_AccountId] FOREIGN KEY ([AccountId]) REFERENCES dbo.[ACCOUNT]([Id]) ON DELETE NO ACTION;
ALTER TABLE dbo.[NOTIFICATION] ADD CONSTRAINT [FK_NOTIFICATION_ACCOUNT_AccountId] FOREIGN KEY ([AccountId]) REFERENCES dbo.[ACCOUNT]([Id]) ON DELETE NO ACTION;
ALTER TABLE dbo.[AISLE] ADD CONSTRAINT [FK_AISLE_FLOOR_FloorId] FOREIGN KEY ([FloorId]) REFERENCES dbo.[FLOOR]([Id]) ON DELETE NO ACTION;
ALTER TABLE dbo.[MAP] ADD CONSTRAINT [FK_MAP_FLOOR_FloorId] FOREIGN KEY ([FloorId]) REFERENCES dbo.[FLOOR]([Id]) ON DELETE NO ACTION;
ALTER TABLE dbo.[BOOK] ADD CONSTRAINT [FK_BOOK_BOOK_CATEGORY_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES dbo.[BOOK_CATEGORY]([Id]) ON DELETE NO ACTION;
ALTER TABLE dbo.[BOOK_ITEM] ADD CONSTRAINT [FK_BOOK_ITEM_BOOK_BookId] FOREIGN KEY ([BookId]) REFERENCES dbo.[BOOK]([Id]) ON DELETE NO ACTION;
ALTER TABLE dbo.[SHELF] ADD CONSTRAINT [FK_SHELF_AISLE_AisleId] FOREIGN KEY ([AisleId]) REFERENCES dbo.[AISLE]([Id]) ON DELETE NO ACTION;
ALTER TABLE dbo.[SHELF_LEVEL] ADD CONSTRAINT [FK_SHELF_LEVEL_SHELF_ShelfId] FOREIGN KEY ([ShelfId]) REFERENCES dbo.[SHELF]([Id]) ON DELETE NO ACTION;
ALTER TABLE dbo.[BOOK_ITEM] ADD CONSTRAINT [FK_BOOK_ITEM_SHELF_LEVEL_ExpectedShelfLevelId] FOREIGN KEY ([ExpectedShelfLevelId]) REFERENCES dbo.[SHELF_LEVEL]([Id]) ON DELETE NO ACTION;
ALTER TABLE dbo.[BOOK_ITEM] ADD CONSTRAINT [FK_BOOK_ITEM_SHELF_LEVEL_CurrentShelfLevelId] FOREIGN KEY ([CurrentShelfLevelId]) REFERENCES dbo.[SHELF_LEVEL]([Id]) ON DELETE NO ACTION;
ALTER TABLE dbo.[ZONE] ADD CONSTRAINT [FK_ZONE_MAP_MapId] FOREIGN KEY ([MapId]) REFERENCES dbo.[MAP]([Id]) ON DELETE NO ACTION;
ALTER TABLE dbo.[ENDPOINT] ADD CONSTRAINT [FK_ENDPOINT_MAP_MapId] FOREIGN KEY ([MapId]) REFERENCES dbo.[MAP]([Id]) ON DELETE NO ACTION;
ALTER TABLE dbo.[ROBOT] ADD CONSTRAINT [FK_ROBOT_MAP_MapId] FOREIGN KEY ([MapId]) REFERENCES dbo.[MAP]([Id]) ON DELETE NO ACTION;
ALTER TABLE dbo.[ROBOT_LOG] ADD CONSTRAINT [FK_ROBOT_LOG_MAP_MapId] FOREIGN KEY ([MapId]) REFERENCES dbo.[MAP]([Id]) ON DELETE NO ACTION;
ALTER TABLE dbo.[SHELF] ADD CONSTRAINT [FK_SHELF_ENDPOINT_StartEndpointId] FOREIGN KEY ([StartEndpointId]) REFERENCES dbo.[ENDPOINT]([Id]) ON DELETE NO ACTION;
ALTER TABLE dbo.[SHELF] ADD CONSTRAINT [FK_SHELF_ENDPOINT_EndEndpointId] FOREIGN KEY ([EndEndpointId]) REFERENCES dbo.[ENDPOINT]([Id]) ON DELETE NO ACTION;
ALTER TABLE dbo.[ENDPOINT_GROUP_MEMBER] ADD CONSTRAINT [FK_ENDPOINT_GROUP_MEMBER_ENDPOINT_EndpointId] FOREIGN KEY ([EndpointId]) REFERENCES dbo.[ENDPOINT]([Id]) ON DELETE NO ACTION;
ALTER TABLE dbo.[ENDPOINT_GROUP_MEMBER] ADD CONSTRAINT [FK_ENDPOINT_GROUP_MEMBER_ENDPOINT_GROUP_EndpointGroupId] FOREIGN KEY ([EndpointGroupId]) REFERENCES dbo.[ENDPOINT_GROUP]([Id]) ON DELETE NO ACTION;
ALTER TABLE dbo.[WORKFLOW_TASK] ADD CONSTRAINT [FK_WORKFLOW_TASK_WORKFLOW_WorkflowId] FOREIGN KEY ([WorkflowId]) REFERENCES dbo.[WORKFLOW]([Id]) ON DELETE NO ACTION;
ALTER TABLE dbo.[JOB] ADD CONSTRAINT [FK_JOB_WORKFLOW_WorkflowId] FOREIGN KEY ([WorkflowId]) REFERENCES dbo.[WORKFLOW]([Id]) ON DELETE NO ACTION;
ALTER TABLE dbo.[SCHEDULE] ADD CONSTRAINT [FK_SCHEDULE_WORKFLOW_WorkflowId] FOREIGN KEY ([WorkflowId]) REFERENCES dbo.[WORKFLOW]([Id]) ON DELETE NO ACTION;
ALTER TABLE dbo.[TASK_STEP] ADD CONSTRAINT [FK_TASK_STEP_WORKFLOW_TASK_WorkflowTaskId] FOREIGN KEY ([WorkflowTaskId]) REFERENCES dbo.[WORKFLOW_TASK]([Id]) ON DELETE NO ACTION;
ALTER TABLE dbo.[JOB_TASK] ADD CONSTRAINT [FK_JOB_TASK_WORKFLOW_TASK_WorkflowTaskId] FOREIGN KEY ([WorkflowTaskId]) REFERENCES dbo.[WORKFLOW_TASK]([Id]) ON DELETE NO ACTION;
ALTER TABLE dbo.[JOB_STEP] ADD CONSTRAINT [FK_JOB_STEP_TASK_STEP_TaskStepId] FOREIGN KEY ([TaskStepId]) REFERENCES dbo.[TASK_STEP]([Id]) ON DELETE NO ACTION;
ALTER TABLE dbo.[JOB_TASK] ADD CONSTRAINT [FK_JOB_TASK_JOB_JobId] FOREIGN KEY ([JobId]) REFERENCES dbo.[JOB]([Id]) ON DELETE NO ACTION;
ALTER TABLE dbo.[JOB_EVENT] ADD CONSTRAINT [FK_JOB_EVENT_JOB_JobId] FOREIGN KEY ([JobId]) REFERENCES dbo.[JOB]([Id]) ON DELETE NO ACTION;
ALTER TABLE dbo.[FEEDBACK] ADD CONSTRAINT [FK_FEEDBACK_JOB_JobId] FOREIGN KEY ([JobId]) REFERENCES dbo.[JOB]([Id]) ON DELETE NO ACTION;
ALTER TABLE dbo.[INVENTORY_REPORT] ADD CONSTRAINT [FK_INVENTORY_REPORT_JOB_JobId] FOREIGN KEY ([JobId]) REFERENCES dbo.[JOB]([Id]) ON DELETE NO ACTION;
ALTER TABLE dbo.[PATROL_REPORT] ADD CONSTRAINT [FK_PATROL_REPORT_JOB_JobId] FOREIGN KEY ([JobId]) REFERENCES dbo.[JOB]([Id]) ON DELETE NO ACTION;
ALTER TABLE dbo.[JOB] ADD CONSTRAINT [FK_JOB_ROBOT_RobotId] FOREIGN KEY ([RobotId]) REFERENCES dbo.[ROBOT]([Id]) ON DELETE NO ACTION;
ALTER TABLE dbo.[ROBOT_LOG] ADD CONSTRAINT [FK_ROBOT_LOG_ROBOT_RobotId] FOREIGN KEY ([RobotId]) REFERENCES dbo.[ROBOT]([Id]) ON DELETE NO ACTION;
ALTER TABLE dbo.[JOB_STEP] ADD CONSTRAINT [FK_JOB_STEP_JOB_TASK_JobTaskId] FOREIGN KEY ([JobTaskId]) REFERENCES dbo.[JOB_TASK]([Id]) ON DELETE NO ACTION;
ALTER TABLE dbo.[BOOK_RECEIPT_DETAIL] ADD CONSTRAINT [FK_BOOK_RECEIPT_DETAIL_BOOK_RECEIPT_ReceiptId] FOREIGN KEY ([ReceiptId]) REFERENCES dbo.[BOOK_RECEIPT]([Id]) ON DELETE NO ACTION;
ALTER TABLE dbo.[BOOK_RECEIPT_DETAIL] ADD CONSTRAINT [FK_BOOK_RECEIPT_DETAIL_BOOK_BookId] FOREIGN KEY ([BookId]) REFERENCES dbo.[BOOK]([Id]) ON DELETE NO ACTION;
ALTER TABLE dbo.[BOOK_ISSUE_DETAIL] ADD CONSTRAINT [FK_BOOK_ISSUE_DETAIL_BOOK_ISSUE_IssueId] FOREIGN KEY ([IssueId]) REFERENCES dbo.[BOOK_ISSUE]([Id]) ON DELETE NO ACTION;
ALTER TABLE dbo.[BOOK_ISSUE_DETAIL] ADD CONSTRAINT [FK_BOOK_ISSUE_DETAIL_BOOK_BookId] FOREIGN KEY ([BookId]) REFERENCES dbo.[BOOK]([Id]) ON DELETE NO ACTION;
ALTER TABLE dbo.[BOOK_RECEIPT_DETAIL] ADD CONSTRAINT [FK_BOOK_RECEIPT_DETAIL_BOOK_ITEM_BookItemId_BookId] FOREIGN KEY ([BookItemId], [BookId]) REFERENCES dbo.[BOOK_ITEM]([Id], [BookId]) ON DELETE NO ACTION;
ALTER TABLE dbo.[BOOK_ISSUE_DETAIL] ADD CONSTRAINT [FK_BOOK_ISSUE_DETAIL_BOOK_ITEM_BookItemId_BookId] FOREIGN KEY ([BookItemId], [BookId]) REFERENCES dbo.[BOOK_ITEM]([Id], [BookId]) ON DELETE NO ACTION;
ALTER TABLE dbo.[INVENTORY_REPORT_DETAIL] ADD CONSTRAINT [FK_INVENTORY_REPORT_DETAIL_INVENTORY_REPORT_ReportId] FOREIGN KEY ([ReportId]) REFERENCES dbo.[INVENTORY_REPORT]([Id]) ON DELETE NO ACTION;
ALTER TABLE dbo.[INVENTORY_REPORT_DETAIL] ADD CONSTRAINT [FK_INVENTORY_REPORT_DETAIL_BOOK_ITEM_BookItemId] FOREIGN KEY ([BookItemId]) REFERENCES dbo.[BOOK_ITEM]([Id]) ON DELETE NO ACTION;
ALTER TABLE dbo.[INVENTORY_REPORT_DETAIL] ADD CONSTRAINT [FK_INVENTORY_REPORT_DETAIL_SHELF_LEVEL_ExpectedShelfLevelId] FOREIGN KEY ([ExpectedShelfLevelId]) REFERENCES dbo.[SHELF_LEVEL]([Id]) ON DELETE NO ACTION;
ALTER TABLE dbo.[INVENTORY_REPORT_DETAIL] ADD CONSTRAINT [FK_INVENTORY_REPORT_DETAIL_SHELF_LEVEL_DetectedShelfLevelId] FOREIGN KEY ([DetectedShelfLevelId]) REFERENCES dbo.[SHELF_LEVEL]([Id]) ON DELETE NO ACTION;
