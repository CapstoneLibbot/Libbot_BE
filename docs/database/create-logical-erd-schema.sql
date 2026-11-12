-- Generated from docs/database/Logical_ERD.corrected.json.
-- The source ERD defines business structure; every FK uses ON DELETE NO ACTION.

CREATE TABLE dbo.[ACCOUNT] (
    [Id] uniqueidentifier NOT NULL,
    [Username] nvarchar(450) NULL,
    [PasswordHash] nvarchar(max) NULL,
    [Role] nvarchar(max) NULL,
    [FullName] nvarchar(max) NOT NULL,
    [Email] nvarchar(450) NOT NULL,
    [Phone] nvarchar(max) NULL,
    [Status] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    [LastLoginAt] datetime2 NULL,
    CONSTRAINT [PK_ACCOUNT] PRIMARY KEY ([Id])
);
GO

CREATE TABLE dbo.[AUDIT_LOG] (
    [Id] uniqueidentifier NOT NULL,
    [Action] nvarchar(max) NOT NULL,
    [EntityType] nvarchar(max) NOT NULL,
    [EntityId] uniqueidentifier NULL,
    [OldValue] nvarchar(max) NULL,
    [NewValue] nvarchar(max) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [AccountId] uniqueidentifier NULL,
    CONSTRAINT [PK_AUDIT_LOG] PRIMARY KEY ([Id])
);
GO

CREATE TABLE dbo.[NOTIFICATION] (
    [Id] uniqueidentifier NOT NULL,
    [AccountId] uniqueidentifier NULL,
    [Type] nvarchar(max) NOT NULL,
    [Title] nvarchar(max) NOT NULL,
    [Message] nvarchar(max) NOT NULL,
    [Severity] nvarchar(max) NOT NULL,
    [IsRead] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [ReadAt] datetime2 NULL,
    CONSTRAINT [PK_NOTIFICATION] PRIMARY KEY ([Id])
);
GO

CREATE TABLE dbo.[BOOK_ITEM] (
    [Id] uniqueidentifier NOT NULL,
    [ExpectedShelfLevelId] uniqueidentifier NULL,
    [CurrentShelfLevelId] uniqueidentifier NULL,
    [BookId] uniqueidentifier NOT NULL,
    [Barcode] nvarchar(450) NOT NULL,
    [LastInventoryAt] datetime2 NULL,
    [Status] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_BOOK_ITEM] PRIMARY KEY ([Id])
);
GO

CREATE TABLE dbo.[AISLE] (
    [Id] uniqueidentifier NOT NULL,
    [FloorId] uniqueidentifier NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NULL,
    [Status] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_AISLE] PRIMARY KEY ([Id])
);
GO

CREATE TABLE dbo.[BOOK] (
    [Id] uniqueidentifier NOT NULL,
    [CategoryId] uniqueidentifier NULL,
    [Isbn] nvarchar(450) NOT NULL,
    [Title] nvarchar(max) NOT NULL,
    [Author] nvarchar(max) NOT NULL,
    [ImageUrl] nvarchar(max) NULL,
    [Description] nvarchar(max) NULL,
    [Publisher] nvarchar(max) NULL,
    [PublicationYear] int NULL,
    CONSTRAINT [PK_BOOK] PRIMARY KEY ([Id])
);
GO

CREATE TABLE dbo.[FLOOR] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [FloorNumber] int NOT NULL,
    [Description] nvarchar(max) NULL,
    [Status] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_FLOOR] PRIMARY KEY ([Id])
);
GO

CREATE TABLE dbo.[SHELF_LEVEL] (
    [Id] uniqueidentifier NOT NULL,
    [ShelfId] uniqueidentifier NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [LevelNumber] int NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_SHELF_LEVEL] PRIMARY KEY ([Id])
);
GO

CREATE TABLE dbo.[ZONE] (
    [Id] uniqueidentifier NOT NULL,
    [MapId] uniqueidentifier NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [ZoneType] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [MaxSpeed     ] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NULL,
    [MinX] float NOT NULL,
    [MinY] float NOT NULL,
    [MaxX] float NOT NULL,
    [MaxY] float NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_ZONE] PRIMARY KEY ([Id])
);
GO

CREATE TABLE dbo.[SHELF] (
    [Id] uniqueidentifier NOT NULL,
    [StartEndpointId] uniqueidentifier NOT NULL,
    [EndEndpointId] uniqueidentifier NOT NULL,
    [AisleId] uniqueidentifier NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NULL,
    [Status] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_SHELF] PRIMARY KEY ([Id])
);
GO

CREATE TABLE dbo.[BOOK_CATEGORY] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NULL,
    CONSTRAINT [PK_BOOK_CATEGORY] PRIMARY KEY ([Id])
);
GO

CREATE TABLE dbo.[ENDPOINT] (
    [Id] uniqueidentifier NOT NULL,
    [MapId] uniqueidentifier NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NULL,
    [EndpointType] nvarchar(max) NOT NULL,
    [PositionX] float NOT NULL,
    [PositionY] float NOT NULL,
    [IsActive] bit NOT NULL,
    [Theta] float NOT NULL,
    CONSTRAINT [PK_ENDPOINT] PRIMARY KEY ([Id])
);
GO

CREATE TABLE dbo.[FEEDBACK] (
    [Id] uniqueidentifier NOT NULL,
    [JobId] uniqueidentifier NOT NULL,
    [Rating] int NOT NULL,
    [Comment] nvarchar(max) NULL,
    CONSTRAINT [PK_FEEDBACK] PRIMARY KEY ([Id])
);
GO

CREATE TABLE dbo.[JOB_EVENT] (
    [Id] uniqueidentifier NOT NULL,
    [JobId] uniqueidentifier NOT NULL,
    [EventType] nvarchar(max) NOT NULL,
    [EventTime] datetime2 NOT NULL,
    [Data] nvarchar(max) NULL,
    CONSTRAINT [PK_JOB_EVENT] PRIMARY KEY ([Id])
);
GO

CREATE TABLE dbo.[ROBOT] (
    [Id] uniqueidentifier NOT NULL,
    [MapId] uniqueidentifier NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [SerialNumber] nvarchar(450) NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [BatteryLevel] float NOT NULL,
    [CreateAt] datetime2 NOT NULL,
    [UpdateAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ROBOT] PRIMARY KEY ([Id])
);
GO

CREATE TABLE dbo.[WORKFLOW] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [Type] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NULL,
    [Status] nvarchar(max) NOT NULL,
    [CreateAt] datetime2 NOT NULL,
    CONSTRAINT [PK_WORKFLOW] PRIMARY KEY ([Id])
);
GO

CREATE TABLE dbo.[WORKFLOW_TASK] (
    [Id] uniqueidentifier NOT NULL,
    [WorkflowId] uniqueidentifier NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NULL,
    [SequenceNo] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_WORKFLOW_TASK] PRIMARY KEY ([Id])
);
GO

CREATE TABLE dbo.[TASK_STEP] (
    [Id] uniqueidentifier NOT NULL,
    [WorkflowTaskId] uniqueidentifier NOT NULL,
    [SequenceNo] int NOT NULL,
    [StepType] nvarchar(max) NOT NULL,
    [Configuration] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_TASK_STEP] PRIMARY KEY ([Id])
);
GO

CREATE TABLE dbo.[ROBOT_LOG] (
    [Id] uniqueidentifier NOT NULL,
    [RobotId] uniqueidentifier NOT NULL,
    [PositionX] float NOT NULL,
    [MapId] uniqueidentifier NOT NULL,
    [PositionY] float NOT NULL,
    [CurrentBatteryLevel] float NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [Data] nvarchar(max) NULL,
    [RecordAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ROBOT_LOG] PRIMARY KEY ([Id])
);
GO

CREATE TABLE dbo.[JOB_TASK] (
    [Id] uniqueidentifier NOT NULL,
    [JobId] uniqueidentifier NOT NULL,
    [WorkflowTaskId] uniqueidentifier NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [StartedAt] datetime2 NULL,
    [CompletedAt] datetime2 NULL,
    [Description] nvarchar(max) NULL,
    CONSTRAINT [PK_JOB_TASK] PRIMARY KEY ([Id])
);
GO

CREATE TABLE dbo.[MAP] (
    [Id] uniqueidentifier NOT NULL,
    [FloorId] uniqueidentifier NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [Version] nvarchar(max) NOT NULL,
    [Width] int NOT NULL,
    [Height] int NOT NULL,
    [MapData] nvarchar(max) NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_MAP] PRIMARY KEY ([Id])
);
GO

CREATE TABLE dbo.[JOB] (
    [Id] uniqueidentifier NOT NULL,
    [WorkflowId] uniqueidentifier NOT NULL,
    [RobotId] uniqueidentifier NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [Priority] int NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NULL,
    [StartedAt] datetime2 NULL,
    [CompletedAt] datetime2 NULL,
    [CreateAt] datetime2 NOT NULL,
    CONSTRAINT [PK_JOB] PRIMARY KEY ([Id])
);
GO

CREATE TABLE dbo.[INVENTORY_REPORT] (
    [Id] uniqueidentifier NOT NULL,
    [JobId] uniqueidentifier NOT NULL,
    [ScopeType] nvarchar(max) NOT NULL,
    [TotalShelves] int NOT NULL,
    [TotalBooksScanned] int NOT NULL,
    [TotalAvailable] int NOT NULL,
    [TotalMisplaced] int NOT NULL,
    [TotalMissing] int NOT NULL,
    [Data] nvarchar(max) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_INVENTORY_REPORT] PRIMARY KEY ([Id])
);
GO

CREATE TABLE dbo.[ENDPOINT_GROUP_MEMBER] (
    [Id] uniqueidentifier NOT NULL,
    [EnpointGroupId] uniqueidentifier NOT NULL,
    [EndpointId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_ENDPOINT_GROUP_MEMBER] PRIMARY KEY ([Id])
);
GO

CREATE TABLE dbo.[ENDPOINT_GROUP] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NULL,
    CONSTRAINT [PK_ENDPOINT_GROUP] PRIMARY KEY ([Id])
);
GO

CREATE TABLE dbo.[PATROL_REPORT] (
    [Id] uniqueidentifier NOT NULL,
    [JobId] uniqueidentifier NOT NULL,
    [StartedAt] datetime2 NULL,
    [CompletedAt] datetime2 NULL,
    [DurationSec] int NOT NULL,
    [ObservationCount] int NOT NULL,
    [Data] nvarchar(max) NULL,
    [WarningCount] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_PATROL_REPORT] PRIMARY KEY ([Id])
);
GO

CREATE TABLE dbo.[JOB_STEP] (
    [Id] uniqueidentifier NOT NULL,
    [JobTaskId] uniqueidentifier NOT NULL,
    [TaskStepId] uniqueidentifier NOT NULL,
    [StepType] nvarchar(max) NOT NULL,
    [Sequence] int NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NULL,
    CONSTRAINT [PK_JOB_STEP] PRIMARY KEY ([Id])
);
GO

CREATE TABLE dbo.[SCHEDULE] (
    [Id] uniqueidentifier NOT NULL,
    [WorkflowId] uniqueidentifier NOT NULL,
    [ScheduleType] nvarchar(max) NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [Data] nvarchar(max) NULL,
    [ScheduleTime] time NOT NULL,
    [StartDate] nvarchar(max) NOT NULL,
    [EndDate] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [LastRunAt] datetime2 NULL,
    [NextRunAt] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_SCHEDULE] PRIMARY KEY ([Id])
);
GO

CREATE TABLE dbo.[BOOK_RECEIPT_DETAIL] (
    [Id] uniqueidentifier NOT NULL,
    [ReceiptId] uniqueidentifier NOT NULL,
    [BookItemId] uniqueidentifier NOT NULL,
    [BookId] uniqueidentifier NOT NULL,
    [Note] nvarchar(max) NULL,
    CONSTRAINT [PK_BOOK_RECEIPT_DETAIL] PRIMARY KEY ([Id])
);
GO

CREATE TABLE dbo.[BOOK_RECEIPT] (
    [Id] uniqueidentifier NOT NULL,
    [BookId] uniqueidentifier NOT NULL,
    [ReceiptType] nvarchar(max) NOT NULL,
    [ReceivedDate] date NULL,
    [Status] nvarchar(max) NOT NULL,
    [Total] int NOT NULL,
    [Note] nvarchar(max) NULL,
    CONSTRAINT [PK_BOOK_RECEIPT] PRIMARY KEY ([Id])
);
GO

CREATE TABLE dbo.[INVENTORY_REPORT_DETAIL] (
    [Id] uniqueidentifier NOT NULL,
    [ReportId] uniqueidentifier NOT NULL,
    [BookItemId] uniqueidentifier NOT NULL,
    [ExpectedShelfLevelId] uniqueidentifier NULL,
    [DetectedShelfLevelId] uniqueidentifier NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [Result] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_INVENTORY_REPORT_DETAIL] PRIMARY KEY ([Id])
);
GO

CREATE TABLE dbo.[BOOK_ISSUE_DETAIL] (
    [Id] uniqueidentifier NOT NULL,
    [IssueId] uniqueidentifier NOT NULL,
    [BookItemId] uniqueidentifier NOT NULL,
    [BookId] uniqueidentifier NOT NULL,
    [Note] nvarchar(max) NULL,
    CONSTRAINT [PK_BOOK_ISSUE_DETAIL] PRIMARY KEY ([Id])
);
GO

CREATE TABLE dbo.[BOOK_ISSUE] (
    [Id] uniqueidentifier NOT NULL,
    [BookId] uniqueidentifier NOT NULL,
    [IssueType] nvarchar(max) NOT NULL,
    [IssueDate] date NULL,
    [Status] nvarchar(max) NOT NULL,
    [Total] int NOT NULL,
    [Note] nvarchar(max) NULL,
    CONSTRAINT [PK_BOOK_ISSUE] PRIMARY KEY ([Id])
);
GO

ALTER TABLE dbo.[AUDIT_LOG] ADD CONSTRAINT [FK_AUDIT_LOG_ACCOUNT_AccountId] FOREIGN KEY ([AccountId]) REFERENCES dbo.[ACCOUNT]([Id]) ON DELETE NO ACTION;
GO
ALTER TABLE dbo.[NOTIFICATION] ADD CONSTRAINT [FK_NOTIFICATION_ACCOUNT_AccountId] FOREIGN KEY ([AccountId]) REFERENCES dbo.[ACCOUNT]([Id]) ON DELETE NO ACTION;
GO
ALTER TABLE dbo.[AISLE] ADD CONSTRAINT [FK_AISLE_FLOOR_FloorId] FOREIGN KEY ([FloorId]) REFERENCES dbo.[FLOOR]([Id]) ON DELETE NO ACTION;
GO
ALTER TABLE dbo.[MAP] ADD CONSTRAINT [FK_MAP_FLOOR_FloorId] FOREIGN KEY ([FloorId]) REFERENCES dbo.[FLOOR]([Id]) ON DELETE NO ACTION;
GO
ALTER TABLE dbo.[BOOK] ADD CONSTRAINT [FK_BOOK_BOOK_CATEGORY_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES dbo.[BOOK_CATEGORY]([Id]) ON DELETE NO ACTION;
GO
ALTER TABLE dbo.[BOOK_ITEM] ADD CONSTRAINT [FK_BOOK_ITEM_BOOK_BookId] FOREIGN KEY ([BookId]) REFERENCES dbo.[BOOK]([Id]) ON DELETE NO ACTION;
GO
ALTER TABLE dbo.[SHELF] ADD CONSTRAINT [FK_SHELF_AISLE_AisleId] FOREIGN KEY ([AisleId]) REFERENCES dbo.[AISLE]([Id]) ON DELETE NO ACTION;
GO
ALTER TABLE dbo.[SHELF_LEVEL] ADD CONSTRAINT [FK_SHELF_LEVEL_SHELF_ShelfId] FOREIGN KEY ([ShelfId]) REFERENCES dbo.[SHELF]([Id]) ON DELETE NO ACTION;
GO
ALTER TABLE dbo.[BOOK_ITEM] ADD CONSTRAINT [FK_BOOK_ITEM_SHELF_LEVEL_ExpectedShelfLevelId] FOREIGN KEY ([ExpectedShelfLevelId]) REFERENCES dbo.[SHELF_LEVEL]([Id]) ON DELETE NO ACTION;
GO
ALTER TABLE dbo.[BOOK_ITEM] ADD CONSTRAINT [FK_BOOK_ITEM_SHELF_LEVEL_CurrentShelfLevelId] FOREIGN KEY ([CurrentShelfLevelId]) REFERENCES dbo.[SHELF_LEVEL]([Id]) ON DELETE NO ACTION;
GO
ALTER TABLE dbo.[ZONE] ADD CONSTRAINT [FK_ZONE_MAP_MapId] FOREIGN KEY ([MapId]) REFERENCES dbo.[MAP]([Id]) ON DELETE NO ACTION;
GO
ALTER TABLE dbo.[ENDPOINT] ADD CONSTRAINT [FK_ENDPOINT_MAP_MapId] FOREIGN KEY ([MapId]) REFERENCES dbo.[MAP]([Id]) ON DELETE NO ACTION;
GO
ALTER TABLE dbo.[ROBOT] ADD CONSTRAINT [FK_ROBOT_MAP_MapId] FOREIGN KEY ([MapId]) REFERENCES dbo.[MAP]([Id]) ON DELETE NO ACTION;
GO
ALTER TABLE dbo.[ROBOT_LOG] ADD CONSTRAINT [FK_ROBOT_LOG_MAP_MapId] FOREIGN KEY ([MapId]) REFERENCES dbo.[MAP]([Id]) ON DELETE NO ACTION;
GO
ALTER TABLE dbo.[SHELF] ADD CONSTRAINT [FK_SHELF_ENDPOINT_StartEndpointId] FOREIGN KEY ([StartEndpointId]) REFERENCES dbo.[ENDPOINT]([Id]) ON DELETE NO ACTION;
GO
ALTER TABLE dbo.[SHELF] ADD CONSTRAINT [FK_SHELF_ENDPOINT_EndEndpointId] FOREIGN KEY ([EndEndpointId]) REFERENCES dbo.[ENDPOINT]([Id]) ON DELETE NO ACTION;
GO
ALTER TABLE dbo.[ENDPOINT_GROUP_MEMBER] ADD CONSTRAINT [FK_ENDPOINT_GROUP_MEMBER_ENDPOINT_EndpointId] FOREIGN KEY ([EndpointId]) REFERENCES dbo.[ENDPOINT]([Id]) ON DELETE NO ACTION;
GO
ALTER TABLE dbo.[ENDPOINT_GROUP_MEMBER] ADD CONSTRAINT [FK_ENDPOINT_GROUP_MEMBER_ENDPOINT_GROUP_EnpointGroupId] FOREIGN KEY ([EnpointGroupId]) REFERENCES dbo.[ENDPOINT_GROUP]([Id]) ON DELETE NO ACTION;
GO
ALTER TABLE dbo.[WORKFLOW_TASK] ADD CONSTRAINT [FK_WORKFLOW_TASK_WORKFLOW_WorkflowId] FOREIGN KEY ([WorkflowId]) REFERENCES dbo.[WORKFLOW]([Id]) ON DELETE NO ACTION;
GO
ALTER TABLE dbo.[JOB] ADD CONSTRAINT [FK_JOB_WORKFLOW_WorkflowId] FOREIGN KEY ([WorkflowId]) REFERENCES dbo.[WORKFLOW]([Id]) ON DELETE NO ACTION;
GO
ALTER TABLE dbo.[SCHEDULE] ADD CONSTRAINT [FK_SCHEDULE_WORKFLOW_WorkflowId] FOREIGN KEY ([WorkflowId]) REFERENCES dbo.[WORKFLOW]([Id]) ON DELETE NO ACTION;
GO
ALTER TABLE dbo.[TASK_STEP] ADD CONSTRAINT [FK_TASK_STEP_WORKFLOW_TASK_WorkflowTaskId] FOREIGN KEY ([WorkflowTaskId]) REFERENCES dbo.[WORKFLOW_TASK]([Id]) ON DELETE NO ACTION;
GO
ALTER TABLE dbo.[JOB_TASK] ADD CONSTRAINT [FK_JOB_TASK_WORKFLOW_TASK_WorkflowTaskId] FOREIGN KEY ([WorkflowTaskId]) REFERENCES dbo.[WORKFLOW_TASK]([Id]) ON DELETE NO ACTION;
GO
ALTER TABLE dbo.[JOB_STEP] ADD CONSTRAINT [FK_JOB_STEP_TASK_STEP_TaskStepId] FOREIGN KEY ([TaskStepId]) REFERENCES dbo.[TASK_STEP]([Id]) ON DELETE NO ACTION;
GO
ALTER TABLE dbo.[JOB_TASK] ADD CONSTRAINT [FK_JOB_TASK_JOB_JobId] FOREIGN KEY ([JobId]) REFERENCES dbo.[JOB]([Id]) ON DELETE NO ACTION;
GO
ALTER TABLE dbo.[JOB_EVENT] ADD CONSTRAINT [FK_JOB_EVENT_JOB_JobId] FOREIGN KEY ([JobId]) REFERENCES dbo.[JOB]([Id]) ON DELETE NO ACTION;
GO
ALTER TABLE dbo.[FEEDBACK] ADD CONSTRAINT [FK_FEEDBACK_JOB_JobId] FOREIGN KEY ([JobId]) REFERENCES dbo.[JOB]([Id]) ON DELETE NO ACTION;
GO
ALTER TABLE dbo.[INVENTORY_REPORT] ADD CONSTRAINT [FK_INVENTORY_REPORT_JOB_JobId] FOREIGN KEY ([JobId]) REFERENCES dbo.[JOB]([Id]) ON DELETE NO ACTION;
GO
ALTER TABLE dbo.[PATROL_REPORT] ADD CONSTRAINT [FK_PATROL_REPORT_JOB_JobId] FOREIGN KEY ([JobId]) REFERENCES dbo.[JOB]([Id]) ON DELETE NO ACTION;
GO
ALTER TABLE dbo.[JOB] ADD CONSTRAINT [FK_JOB_ROBOT_RobotId] FOREIGN KEY ([RobotId]) REFERENCES dbo.[ROBOT]([Id]) ON DELETE NO ACTION;
GO
ALTER TABLE dbo.[ROBOT_LOG] ADD CONSTRAINT [FK_ROBOT_LOG_ROBOT_RobotId] FOREIGN KEY ([RobotId]) REFERENCES dbo.[ROBOT]([Id]) ON DELETE NO ACTION;
GO
ALTER TABLE dbo.[JOB_STEP] ADD CONSTRAINT [FK_JOB_STEP_JOB_TASK_JobTaskId] FOREIGN KEY ([JobTaskId]) REFERENCES dbo.[JOB_TASK]([Id]) ON DELETE NO ACTION;
GO
ALTER TABLE dbo.[BOOK_RECEIPT] ADD CONSTRAINT [FK_BOOK_RECEIPT_BOOK_BookId] FOREIGN KEY ([BookId]) REFERENCES dbo.[BOOK]([Id]) ON DELETE NO ACTION;
GO
ALTER TABLE dbo.[BOOK_RECEIPT_DETAIL] ADD CONSTRAINT [FK_BOOK_RECEIPT_DETAIL_BOOK_RECEIPT_ReceiptId] FOREIGN KEY ([ReceiptId]) REFERENCES dbo.[BOOK_RECEIPT]([Id]) ON DELETE NO ACTION;
GO
ALTER TABLE dbo.[BOOK_RECEIPT_DETAIL] ADD CONSTRAINT [FK_BOOK_RECEIPT_DETAIL_BOOK_BookId] FOREIGN KEY ([BookId]) REFERENCES dbo.[BOOK]([Id]) ON DELETE NO ACTION;
GO
ALTER TABLE dbo.[BOOK_RECEIPT_DETAIL] ADD CONSTRAINT [FK_BOOK_RECEIPT_DETAIL_BOOK_ITEM_BookItemId] FOREIGN KEY ([BookItemId]) REFERENCES dbo.[BOOK_ITEM]([Id]) ON DELETE NO ACTION;
GO
ALTER TABLE dbo.[BOOK_ISSUE] ADD CONSTRAINT [FK_BOOK_ISSUE_BOOK_BookId] FOREIGN KEY ([BookId]) REFERENCES dbo.[BOOK]([Id]) ON DELETE NO ACTION;
GO
ALTER TABLE dbo.[BOOK_ISSUE_DETAIL] ADD CONSTRAINT [FK_BOOK_ISSUE_DETAIL_BOOK_ISSUE_IssueId] FOREIGN KEY ([IssueId]) REFERENCES dbo.[BOOK_ISSUE]([Id]) ON DELETE NO ACTION;
GO
ALTER TABLE dbo.[BOOK_ISSUE_DETAIL] ADD CONSTRAINT [FK_BOOK_ISSUE_DETAIL_BOOK_BookId] FOREIGN KEY ([BookId]) REFERENCES dbo.[BOOK]([Id]) ON DELETE NO ACTION;
GO
ALTER TABLE dbo.[BOOK_ISSUE_DETAIL] ADD CONSTRAINT [FK_BOOK_ISSUE_DETAIL_BOOK_ITEM_BookItemId] FOREIGN KEY ([BookItemId]) REFERENCES dbo.[BOOK_ITEM]([Id]) ON DELETE NO ACTION;
GO
ALTER TABLE dbo.[INVENTORY_REPORT_DETAIL] ADD CONSTRAINT [FK_INVENTORY_REPORT_DETAIL_INVENTORY_REPORT_ReportId] FOREIGN KEY ([ReportId]) REFERENCES dbo.[INVENTORY_REPORT]([Id]) ON DELETE NO ACTION;
GO
ALTER TABLE dbo.[INVENTORY_REPORT_DETAIL] ADD CONSTRAINT [FK_INVENTORY_REPORT_DETAIL_BOOK_ITEM_BookItemId] FOREIGN KEY ([BookItemId]) REFERENCES dbo.[BOOK_ITEM]([Id]) ON DELETE NO ACTION;
GO
ALTER TABLE dbo.[INVENTORY_REPORT_DETAIL] ADD CONSTRAINT [FK_INVENTORY_REPORT_DETAIL_SHELF_LEVEL_ExpectedShelfLevelId] FOREIGN KEY ([ExpectedShelfLevelId]) REFERENCES dbo.[SHELF_LEVEL]([Id]) ON DELETE NO ACTION;
GO
ALTER TABLE dbo.[INVENTORY_REPORT_DETAIL] ADD CONSTRAINT [FK_INVENTORY_REPORT_DETAIL_SHELF_LEVEL_DetectedShelfLevelId] FOREIGN KEY ([DetectedShelfLevelId]) REFERENCES dbo.[SHELF_LEVEL]([Id]) ON DELETE NO ACTION;
GO
