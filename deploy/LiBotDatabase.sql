IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE TABLE [ACCOUNT] (
        [Id] uniqueidentifier NOT NULL,
        [Username] nvarchar(450) NULL,
        [PasswordHash] nvarchar(max) NULL,
        [Role] nvarchar(max) NULL,
        [Email] nvarchar(450) NOT NULL,
        [FullName] nvarchar(max) NOT NULL,
        [Phone] nvarchar(max) NULL,
        [Status] nvarchar(20) NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [LastLoginAtUtc] datetime2 NULL,
        CONSTRAINT [PK_ACCOUNT] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE TABLE [BOOK_CATEGORY] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_BOOK_CATEGORY] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE TABLE [ENDPOINT_GROUP] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_ENDPOINT_GROUP] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE TABLE [FLOOR] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [FloorNumber] int NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_FLOOR] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE TABLE [INVENTORY_ITEM] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [Quantity] int NOT NULL,
        [Unit] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_INVENTORY_ITEM] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE TABLE [MAP] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [Version] nvarchar(max) NOT NULL,
        [Width] int NOT NULL,
        [Height] int NOT NULL,
        [MapData] nvarchar(max) NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_MAP] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE TABLE [REPORT] (
        [Id] uniqueidentifier NOT NULL,
        [ReportType] nvarchar(max) NOT NULL,
        [Summary] nvarchar(max) NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_REPORT] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE TABLE [ROBOT] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [SerialNumber] nvarchar(450) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [BatteryLevel] float NOT NULL,
        CONSTRAINT [PK_ROBOT] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE TABLE [WORKFLOW] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_WORKFLOW] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE TABLE [AUDIT_LOG] (
        [Id] uniqueidentifier NOT NULL,
        [AccountId] uniqueidentifier NULL,
        [Action] nvarchar(max) NOT NULL,
        [EntityType] nvarchar(max) NOT NULL,
        [EntityId] uniqueidentifier NULL,
        [OldValue] nvarchar(max) NULL,
        [NewValue] nvarchar(max) NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_AUDIT_LOG] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AUDIT_LOG_ACCOUNT_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [ACCOUNT] ([Id]) ON DELETE SET NULL
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE TABLE [NOTIFICATION] (
        [Id] uniqueidentifier NOT NULL,
        [AccountId] uniqueidentifier NULL,
        [Type] nvarchar(max) NOT NULL,
        [Title] nvarchar(max) NOT NULL,
        [Message] nvarchar(max) NOT NULL,
        [Severity] nvarchar(max) NOT NULL,
        [IsRead] bit NOT NULL,
        [ReadAtUtc] datetime2 NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_NOTIFICATION] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_NOTIFICATION_ACCOUNT_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [ACCOUNT] ([Id]) ON DELETE SET NULL
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE TABLE [SCHEDULE] (
        [Id] uniqueidentifier NOT NULL,
        [CreatedBy] uniqueidentifier NULL,
        [ScheduleType] nvarchar(max) NOT NULL,
        [ScheduleTime] time NOT NULL,
        [StartDate] date NOT NULL,
        [EndDate] date NULL,
        [LastRunAtUtc] datetime2 NULL,
        [NextRunAtUtc] datetime2 NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_SCHEDULE] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SCHEDULE_ACCOUNT_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [ACCOUNT] ([Id]) ON DELETE SET NULL
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE TABLE [BOOK] (
        [Id] uniqueidentifier NOT NULL,
        [CategoryId] uniqueidentifier NULL,
        [Isbn] nvarchar(450) NOT NULL,
        [Title] nvarchar(max) NOT NULL,
        [Author] nvarchar(max) NOT NULL,
        [Publisher] nvarchar(max) NULL,
        [PublicationYear] int NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_BOOK] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_BOOK_BOOK_CATEGORY_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [BOOK_CATEGORY] ([Id]) ON DELETE SET NULL
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE TABLE [AREA] (
        [Id] uniqueidentifier NOT NULL,
        [FloorId] uniqueidentifier NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_AREA] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AREA_FLOOR_FloorId] FOREIGN KEY ([FloorId]) REFERENCES [FLOOR] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE TABLE [ENDPOINT] (
        [Id] uniqueidentifier NOT NULL,
        [MapId] uniqueidentifier NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [PositionX] float NOT NULL,
        [PositionY] float NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_ENDPOINT] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ENDPOINT_MAP_MapId] FOREIGN KEY ([MapId]) REFERENCES [MAP] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE TABLE [ZONE] (
        [Id] uniqueidentifier NOT NULL,
        [MapId] uniqueidentifier NOT NULL,
        [ZoneType] nvarchar(max) NOT NULL,
        [MaxSpeed] float NOT NULL,
        [PreferredDirection] nvarchar(max) NULL,
        [CorridorWidth] float NULL,
        [AllowTurnaround] bit NOT NULL,
        [MinX] float NOT NULL,
        [MinY] float NOT NULL,
        [MaxX] float NOT NULL,
        [MaxY] float NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_ZONE] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ZONE_MAP_MapId] FOREIGN KEY ([MapId]) REFERENCES [MAP] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE TABLE [INVENTORY_REPORT] (
        [Id] uniqueidentifier NOT NULL,
        [ReportId] uniqueidentifier NOT NULL,
        [ScopeType] nvarchar(max) NOT NULL,
        [TotalShelves] int NOT NULL,
        [TotalBooksScanned] int NOT NULL,
        [TotalAvailable] int NOT NULL,
        [TotalMisplaced] int NOT NULL,
        [TotalMissing] int NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_INVENTORY_REPORT] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_INVENTORY_REPORT_REPORT_ReportId] FOREIGN KEY ([ReportId]) REFERENCES [REPORT] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE TABLE [PATROL_REPORT] (
        [Id] uniqueidentifier NOT NULL,
        [ReportId] uniqueidentifier NOT NULL,
        [DurationSeconds] int NOT NULL,
        [ObservationCount] int NOT NULL,
        [WarningCount] int NOT NULL,
        [FallenBookCount] int NOT NULL,
        [UnknownObstacleCount] int NOT NULL,
        [OtherWarningCount] int NOT NULL,
        [PatrolStatus] nvarchar(max) NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_PATROL_REPORT] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PATROL_REPORT_REPORT_ReportId] FOREIGN KEY ([ReportId]) REFERENCES [REPORT] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE TABLE [CONFIG_LOG] (
        [Id] uniqueidentifier NOT NULL,
        [RobotId] uniqueidentifier NOT NULL,
        [AccountId] uniqueidentifier NULL,
        [ConfigType] nvarchar(max) NOT NULL,
        [OldValue] nvarchar(max) NULL,
        [NewValue] nvarchar(max) NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_CONFIG_LOG] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CONFIG_LOG_ACCOUNT_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [ACCOUNT] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_CONFIG_LOG_ROBOT_RobotId] FOREIGN KEY ([RobotId]) REFERENCES [ROBOT] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE TABLE [ROBOT_LOG] (
        [Id] uniqueidentifier NOT NULL,
        [RobotId] uniqueidentifier NOT NULL,
        [CurrentBatteryLevel] float NOT NULL,
        [RecordedAtUtc] datetime2 NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_ROBOT_LOG] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ROBOT_LOG_ROBOT_RobotId] FOREIGN KEY ([RobotId]) REFERENCES [ROBOT] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE TABLE [WORKFLOW_TASK] (
        [Id] uniqueidentifier NOT NULL,
        [WorkflowId] uniqueidentifier NOT NULL,
        [SequenceNumber] int NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_WORKFLOW_TASK] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WORKFLOW_TASK_WORKFLOW_WorkflowId] FOREIGN KEY ([WorkflowId]) REFERENCES [WORKFLOW] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE TABLE [AISLE] (
        [Id] uniqueidentifier NOT NULL,
        [FloorId] uniqueidentifier NOT NULL,
        [EndpointId] uniqueidentifier NULL,
        [Code] nvarchar(max) NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_AISLE] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AISLE_ENDPOINT_EndpointId] FOREIGN KEY ([EndpointId]) REFERENCES [ENDPOINT] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_AISLE_FLOOR_FloorId] FOREIGN KEY ([FloorId]) REFERENCES [FLOOR] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE TABLE [ENDPOINT_GROUP_MEMBER] (
        [Id] uniqueidentifier NOT NULL,
        [EndpointGroupId] uniqueidentifier NOT NULL,
        [EndpointId] uniqueidentifier NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_ENDPOINT_GROUP_MEMBER] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ENDPOINT_GROUP_MEMBER_ENDPOINT_EndpointId] FOREIGN KEY ([EndpointId]) REFERENCES [ENDPOINT] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ENDPOINT_GROUP_MEMBER_ENDPOINT_GROUP_EndpointGroupId] FOREIGN KEY ([EndpointGroupId]) REFERENCES [ENDPOINT_GROUP] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE TABLE [JOB] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [CreatedBy] uniqueidentifier NULL,
        [SourceEndpointId] uniqueidentifier NULL,
        [DestinationEndpointId] uniqueidentifier NULL,
        [Priority] int NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [StartedAtUtc] datetime2 NULL,
        [CompletedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_JOB] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_JOB_ACCOUNT_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [ACCOUNT] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_JOB_ENDPOINT_DestinationEndpointId] FOREIGN KEY ([DestinationEndpointId]) REFERENCES [ENDPOINT] ([Id]),
        CONSTRAINT [FK_JOB_ENDPOINT_SourceEndpointId] FOREIGN KEY ([SourceEndpointId]) REFERENCES [ENDPOINT] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE TABLE [TASK_STEP] (
        [Id] uniqueidentifier NOT NULL,
        [WorkflowTaskId] uniqueidentifier NOT NULL,
        [StepType] nvarchar(max) NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_TASK_STEP] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TASK_STEP_WORKFLOW_TASK_WorkflowTaskId] FOREIGN KEY ([WorkflowTaskId]) REFERENCES [WORKFLOW_TASK] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE TABLE [SHELF] (
        [Id] uniqueidentifier NOT NULL,
        [AisleId] uniqueidentifier NOT NULL,
        [Code] nvarchar(max) NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_SHELF] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SHELF_AISLE_AisleId] FOREIGN KEY ([AisleId]) REFERENCES [AISLE] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE TABLE [FEEDBACK] (
        [Id] uniqueidentifier NOT NULL,
        [JobId] uniqueidentifier NOT NULL,
        [Rating] int NOT NULL,
        [Comment] nvarchar(max) NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_FEEDBACK] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FEEDBACK_JOB_JobId] FOREIGN KEY ([JobId]) REFERENCES [JOB] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE TABLE [JOB_EVENT] (
        [Id] uniqueidentifier NOT NULL,
        [JobId] uniqueidentifier NOT NULL,
        [EventType] nvarchar(max) NOT NULL,
        [EventTimeUtc] datetime2 NOT NULL,
        [Data] nvarchar(max) NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_JOB_EVENT] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_JOB_EVENT_JOB_JobId] FOREIGN KEY ([JobId]) REFERENCES [JOB] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE TABLE [JOB_TASK] (
        [Id] uniqueidentifier NOT NULL,
        [JobId] uniqueidentifier NOT NULL,
        [RobotId] uniqueidentifier NULL,
        [StartedAtUtc] datetime2 NULL,
        [CompletedAtUtc] datetime2 NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_JOB_TASK] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_JOB_TASK_JOB_JobId] FOREIGN KEY ([JobId]) REFERENCES [JOB] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_JOB_TASK_ROBOT_RobotId] FOREIGN KEY ([RobotId]) REFERENCES [ROBOT] ([Id]) ON DELETE SET NULL
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE TABLE [SCHEDULE_JOB] (
        [Id] uniqueidentifier NOT NULL,
        [ScheduleId] uniqueidentifier NOT NULL,
        [JobId] uniqueidentifier NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_SCHEDULE_JOB] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SCHEDULE_JOB_JOB_JobId] FOREIGN KEY ([JobId]) REFERENCES [JOB] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_SCHEDULE_JOB_SCHEDULE_ScheduleId] FOREIGN KEY ([ScheduleId]) REFERENCES [SCHEDULE] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE TABLE [SHELF_LEVEL] (
        [Id] uniqueidentifier NOT NULL,
        [ShelfId] uniqueidentifier NOT NULL,
        [LevelNumber] int NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_SHELF_LEVEL] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SHELF_LEVEL_SHELF_ShelfId] FOREIGN KEY ([ShelfId]) REFERENCES [SHELF] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE TABLE [JOB_STEP] (
        [Id] uniqueidentifier NOT NULL,
        [JobTaskId] uniqueidentifier NOT NULL,
        [TaskStepId] uniqueidentifier NOT NULL,
        [Sequence] int NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_JOB_STEP] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_JOB_STEP_JOB_TASK_JobTaskId] FOREIGN KEY ([JobTaskId]) REFERENCES [JOB_TASK] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_JOB_STEP_TASK_STEP_TaskStepId] FOREIGN KEY ([TaskStepId]) REFERENCES [TASK_STEP] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE TABLE [SCHEDULE_JOBTASK] (
        [Id] uniqueidentifier NOT NULL,
        [ScheduledJobId] uniqueidentifier NOT NULL,
        [WorkflowTaskId] uniqueidentifier NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_SCHEDULE_JOBTASK] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SCHEDULE_JOBTASK_SCHEDULE_JOB_ScheduledJobId] FOREIGN KEY ([ScheduledJobId]) REFERENCES [SCHEDULE_JOB] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_SCHEDULE_JOBTASK_WORKFLOW_TASK_WorkflowTaskId] FOREIGN KEY ([WorkflowTaskId]) REFERENCES [WORKFLOW_TASK] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE TABLE [BOOK_ITEM] (
        [Id] uniqueidentifier NOT NULL,
        [BookId] uniqueidentifier NOT NULL,
        [Barcode] nvarchar(450) NOT NULL,
        [ExpectedShelfLevelId] uniqueidentifier NULL,
        [CurrentShelfLevelId] uniqueidentifier NULL,
        [LastInventoryAtUtc] datetime2 NULL,
        [Status] nvarchar(max) NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_BOOK_ITEM] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_BOOK_ITEM_BOOK_BookId] FOREIGN KEY ([BookId]) REFERENCES [BOOK] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_BOOK_ITEM_SHELF_LEVEL_CurrentShelfLevelId] FOREIGN KEY ([CurrentShelfLevelId]) REFERENCES [SHELF_LEVEL] ([Id]),
        CONSTRAINT [FK_BOOK_ITEM_SHELF_LEVEL_ExpectedShelfLevelId] FOREIGN KEY ([ExpectedShelfLevelId]) REFERENCES [SHELF_LEVEL] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE TABLE [SCHEDULE_JOBSTEP] (
        [Id] uniqueidentifier NOT NULL,
        [ScheduledJobTaskId] uniqueidentifier NOT NULL,
        [Parameters] nvarchar(max) NULL,
        [Data] nvarchar(max) NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_SCHEDULE_JOBSTEP] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SCHEDULE_JOBSTEP_SCHEDULE_JOBTASK_ScheduledJobTaskId] FOREIGN KEY ([ScheduledJobTaskId]) REFERENCES [SCHEDULE_JOBTASK] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ACCOUNT_Email] ON [ACCOUNT] ([Email]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_ACCOUNT_Username] ON [ACCOUNT] ([Username]) WHERE [Username] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AISLE_EndpointId] ON [AISLE] ([EndpointId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AISLE_FloorId] ON [AISLE] ([FloorId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AREA_FloorId] ON [AREA] ([FloorId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AUDIT_LOG_AccountId] ON [AUDIT_LOG] ([AccountId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_BOOK_CategoryId] ON [BOOK] ([CategoryId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_BOOK_Isbn] ON [BOOK] ([Isbn]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_BOOK_ITEM_Barcode] ON [BOOK_ITEM] ([Barcode]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_BOOK_ITEM_BookId] ON [BOOK_ITEM] ([BookId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_BOOK_ITEM_CurrentShelfLevelId] ON [BOOK_ITEM] ([CurrentShelfLevelId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_BOOK_ITEM_ExpectedShelfLevelId] ON [BOOK_ITEM] ([ExpectedShelfLevelId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CONFIG_LOG_AccountId] ON [CONFIG_LOG] ([AccountId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CONFIG_LOG_RobotId] ON [CONFIG_LOG] ([RobotId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ENDPOINT_MapId] ON [ENDPOINT] ([MapId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ENDPOINT_GROUP_MEMBER_EndpointGroupId_EndpointId] ON [ENDPOINT_GROUP_MEMBER] ([EndpointGroupId], [EndpointId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ENDPOINT_GROUP_MEMBER_EndpointId] ON [ENDPOINT_GROUP_MEMBER] ([EndpointId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_FEEDBACK_JobId] ON [FEEDBACK] ([JobId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_INVENTORY_REPORT_ReportId] ON [INVENTORY_REPORT] ([ReportId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_JOB_CreatedBy] ON [JOB] ([CreatedBy]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_JOB_DestinationEndpointId] ON [JOB] ([DestinationEndpointId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_JOB_SourceEndpointId] ON [JOB] ([SourceEndpointId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_JOB_EVENT_JobId] ON [JOB_EVENT] ([JobId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_JOB_STEP_JobTaskId] ON [JOB_STEP] ([JobTaskId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_JOB_STEP_TaskStepId] ON [JOB_STEP] ([TaskStepId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_JOB_TASK_JobId] ON [JOB_TASK] ([JobId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_JOB_TASK_RobotId] ON [JOB_TASK] ([RobotId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_NOTIFICATION_AccountId] ON [NOTIFICATION] ([AccountId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PATROL_REPORT_ReportId] ON [PATROL_REPORT] ([ReportId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ROBOT_SerialNumber] ON [ROBOT] ([SerialNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ROBOT_LOG_RobotId] ON [ROBOT_LOG] ([RobotId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_SCHEDULE_CreatedBy] ON [SCHEDULE] ([CreatedBy]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_SCHEDULE_JOB_JobId] ON [SCHEDULE_JOB] ([JobId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_SCHEDULE_JOB_ScheduleId] ON [SCHEDULE_JOB] ([ScheduleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_SCHEDULE_JOBSTEP_ScheduledJobTaskId] ON [SCHEDULE_JOBSTEP] ([ScheduledJobTaskId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_SCHEDULE_JOBTASK_ScheduledJobId] ON [SCHEDULE_JOBTASK] ([ScheduledJobId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_SCHEDULE_JOBTASK_WorkflowTaskId] ON [SCHEDULE_JOBTASK] ([WorkflowTaskId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_SHELF_AisleId] ON [SHELF] ([AisleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_SHELF_LEVEL_ShelfId] ON [SHELF_LEVEL] ([ShelfId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TASK_STEP_WorkflowTaskId] ON [TASK_STEP] ([WorkflowTaskId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_WORKFLOW_TASK_WorkflowId] ON [WORKFLOW_TASK] ([WorkflowId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ZONE_MapId] ON [ZONE] ([MapId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004122647_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261004122647_InitialCreate', N'8.0.31');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006150000_RemoveInventoryItem'
)
BEGIN
    DROP TABLE [INVENTORY_ITEM];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006150000_RemoveInventoryItem'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261006150000_RemoveInventoryItem', N'8.0.31');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009100000_AddWorkflowMissionFields'
)
BEGIN
    ALTER TABLE [JOB] ADD [WorkflowId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009100000_AddWorkflowMissionFields'
)
BEGIN
    ALTER TABLE [WORKFLOW] ADD [Type] nvarchar(max) NOT NULL DEFAULT N'GENERAL';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009100000_AddWorkflowMissionFields'
)
BEGIN
    ALTER TABLE [WORKFLOW] ADD [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009100000_AddWorkflowMissionFields'
)
BEGIN
    ALTER TABLE [TASK_STEP] ADD [Configuration] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009100000_AddWorkflowMissionFields'
)
BEGIN
    CREATE INDEX [IX_JOB_WorkflowId] ON [JOB] ([WorkflowId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009100000_AddWorkflowMissionFields'
)
BEGIN
    ALTER TABLE [JOB] ADD CONSTRAINT [FK_JOB_WORKFLOW_WorkflowId] FOREIGN KEY ([WorkflowId]) REFERENCES [WORKFLOW] ([Id]) ON DELETE SET NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009100000_AddWorkflowMissionFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261009100000_AddWorkflowMissionFields', N'8.0.31');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014010000_AddEndpointThetaAndDirectGuidance'
)
BEGIN
    ALTER TABLE [ENDPOINT] ADD [Theta] float NOT NULL DEFAULT 0.0E0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014010000_AddEndpointThetaAndDirectGuidance'
)
BEGIN
    DECLARE @SeedWorkflowId uniqueidentifier = (
        SELECT TOP (1) Id
        FROM WORKFLOW
        WHERE Type = 'SEARCH_AND_GUIDE_TO_BOOK'
        ORDER BY Id);
    DECLARE @SeedNewWorkflow bit = 0;

    IF @SeedWorkflowId IS NULL
    BEGIN
        SET @SeedWorkflowId = NEWID();
        SET @SeedNewWorkflow = 1;
        INSERT INTO WORKFLOW (Id, Name, Description, Type, IsActive)
        VALUES (
            @SeedWorkflowId,
            N'Tìm và hướng dẫn đến sách',
            N'Luồng 1: tìm sách, hiển thị vị trí và dẫn người đọc đến đúng kệ.',
            'SEARCH_AND_GUIDE_TO_BOOK',
            1);
    END;

    IF @SeedNewWorkflow = 1
    BEGIN
        DECLARE @Definitions TABLE
        (
            SequenceNumber int NOT NULL,
            StepName nvarchar(32) NOT NULL,
            StepType nvarchar(64) NOT NULL,
            Configuration nvarchar(max) NOT NULL
        );

        INSERT INTO @Definitions (SequenceNumber, StepName, StepType, Configuration)
        VALUES
        (1, 'S10', 'PROCESS_DATA', N'{"action":"SEARCH_BOOK","payload":{"query":"${query}","match_mode":"FUZZY","runtime_id":"${runtime_id}"},"output_mapping":{"data.books":"search_results"},"transitions":{"on_success":"S20","on_error":"S210"}}'),
        (2, 'S20', 'DISPLAY', N'{"ui_screen":"BOOK_SEARCH_RESULTS","data":{"books":"${search_results}"},"transitions":{"on_success":"S30","on_error":"S210"}}'),
        (3, 'S30', 'WAIT', N'{"wait_for":"USER_TOUCH","event_type":"BOOK_SELECTED","result_variable":"selected_book_id","timeout_sec":120,"transitions":{"on_event":"S40","on_timeout":"S210","on_cancel":"S210"}}'),
        (4, 'S40', 'PROCESS_DATA', N'{"action":"GET_BOOK_DETAIL","payload":{"book_id":"${selected_book_id}","runtime_id":"${runtime_id}"},"output_mapping":{"data":"book_detail"},"transitions":{"on_success":"S50","on_error":"S210"}}'),
        (5, 'S50', 'DISPLAY', N'{"ui_screen":"BOOK_INFO","data":"${book_detail}","transitions":{"on_success":"S60","on_error":"S210"}}'),
        (6, 'S60', 'WAIT', N'{"wait_for":"USER_TOUCH","event_type":"BOOK_ACTION","allowed_values":["VIEW_LOCATION","START_GUIDANCE","RATE"],"result_variable":"book_action","timeout_sec":120,"transitions":{"on_event":"S70","on_timeout":"S160","on_cancel":"S160"}}'),
        (7, 'S70', 'EVALUATE', N'{"condition_type":"DATA_MATCH","left":"${book_action}","operator":"EQUAL","right":"RATE","transitions":{"on_true":"S160","on_false":"S75"}}'),
        (8, 'S75', 'EVALUATE', N'{"condition_type":"DATA_MATCH","left":"${book_action}","operator":"EQUAL","right":"START_GUIDANCE","transitions":{"on_true":"S110","on_false":"S80"}}'),
        (9, 'S80', 'DISPLAY', N'{"ui_screen":"MAP_LOCATION","data":"${book_detail.location}","transitions":{"on_success":"S90","on_error":"S210"}}'),
        (10, 'S90', 'WAIT', N'{"wait_for":"USER_TOUCH","event_type":"MAP_ACTION","allowed_values":["START_GUIDANCE","RATE"],"result_variable":"map_action","timeout_sec":120,"transitions":{"on_event":"S100","on_timeout":"S160","on_cancel":"S160"}}'),
        (11, 'S100', 'EVALUATE', N'{"condition_type":"DATA_MATCH","left":"${map_action}","operator":"EQUAL","right":"RATE","transitions":{"on_true":"S160","on_false":"S110"}}'),
        (12, 'S110', 'DISPLAY', N'{"ui_screen":"NAV_GUIDE","data":{"destination":"${book_detail.location}","show_route":true},"transitions":{"on_success":"S120","on_error":"S210"}}'),
        (13, 'S120', 'MOVE', N'{"target":"${book_detail.location.endpoint}","speed_mode":"SLOW_GUIDE","obstacle_policy":"WAIT_RESUME","timeout_sec":180,"output_mapping":{"move_status":"move_status","final_pose":"current_pose"},"transitions":{"on_success":"S130","on_error":"S210","on_timeout":"S210"}}'),
        (14, 'S130', 'DISPLAY', N'{"ui_screen":"ARRIVED","data":"${book_detail.location}","transitions":{"on_success":"S140","on_error":"S210"}}'),
        (15, 'S140', 'NOTIFY', N'{"channel":"SPEAKER_TTS","message_type":"INFO","content":"Đã đến vị trí sách. Vui lòng xem kệ và tầng kệ hiển thị trên màn hình.","transitions":{"on_success":"S150","on_error":"S210"}}'),
        (16, 'S150', 'WAIT', N'{"wait_for":"TIMEOUT","timeout_sec":15,"transitions":{"on_timeout":"S160","on_success":"S160"}}'),
        (17, 'S160', 'DISPLAY', N'{"ui_screen":"RATING","data":{"prompt":"Đánh giá trải nghiệm của bạn"},"transitions":{"on_success":"S170","on_error":"S210"}}'),
        (18, 'S170', 'WAIT', N'{"wait_for":"USER_TOUCH","event_type":"RATING_SUBMIT","result_variable":"rating","timeout_sec":10,"transitions":{"on_event":"S180","on_timeout":"S190","on_cancel":"S190"}}'),
        (19, 'S180', 'PROCESS_DATA', N'{"action":"SAVE_RATING","payload":{"rating":"${rating}","task_id":"${task_id}","runtime_id":"${runtime_id}"},"transitions":{"on_success":"S190","on_error":"S210"}}'),
        (20, 'S190', 'DISPLAY', N'{"ui_screen":"HOME","data":{},"transitions":{"on_success":"S200","on_error":"S210"}}'),
        (21, 'S200', 'END', N'{"result_status":"SUCCEEDED"}'),
        (22, 'S210', 'END', N'{"result_status":"FAILED"}');

        DECLARE @SeedSequence int;
        DECLARE @SeedStepName nvarchar(32);
        DECLARE @SeedStepType nvarchar(64);
        DECLARE @SeedConfiguration nvarchar(max);
        DECLARE seed_step_cursor CURSOR LOCAL FAST_FORWARD FOR
            SELECT SequenceNumber, StepName, StepType, Configuration
            FROM @Definitions
            ORDER BY SequenceNumber;

        OPEN seed_step_cursor;
        FETCH NEXT FROM seed_step_cursor
            INTO @SeedSequence, @SeedStepName, @SeedStepType, @SeedConfiguration;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            DECLARE @SeedTaskId uniqueidentifier = NEWID();
            INSERT INTO WORKFLOW_TASK
                (Id, WorkflowId, SequenceNumber, Name, CreatedAtUtc, UpdatedAtUtc)
            VALUES
                (@SeedTaskId, @SeedWorkflowId, @SeedSequence, @SeedStepName, SYSUTCDATETIME(), NULL);
            INSERT INTO TASK_STEP
                (Id, WorkflowTaskId, StepType, CreatedAtUtc, UpdatedAtUtc, Configuration)
            VALUES
                (NEWID(), @SeedTaskId, @SeedStepType, SYSUTCDATETIME(), NULL, @SeedConfiguration);

            FETCH NEXT FROM seed_step_cursor
                INTO @SeedSequence, @SeedStepName, @SeedStepType, @SeedConfiguration;
        END;
        CLOSE seed_step_cursor;
        DEALLOCATE seed_step_cursor;
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014010000_AddEndpointThetaAndDirectGuidance'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261014010000_AddEndpointThetaAndDirectGuidance', N'8.0.31');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015100000_AddBookImage'
)
BEGIN
    ALTER TABLE [BOOK] ADD [Image] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015100000_AddBookImage'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261015100000_AddBookImage', N'8.0.31');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019100000_AddWorkflowRuntimeState'
)
BEGIN
    ALTER TABLE [JOB_TASK] ADD [RuntimeId] nvarchar(32) NOT NULL DEFAULT N'real';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019100000_AddWorkflowRuntimeState'
)
BEGIN
    ALTER TABLE [JOB_TASK] ADD [ExecutionId] nvarchar(64) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019100000_AddWorkflowRuntimeState'
)
BEGIN
    ALTER TABLE [JOB_TASK] ADD [ExecutionState] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019100000_AddWorkflowRuntimeState'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261019100000_AddWorkflowRuntimeState', N'8.0.31');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108042851_UpdateFloorAndAisleStatus'
)
BEGIN
    ALTER TABLE [AISLE] DROP CONSTRAINT [FK_AISLE_ENDPOINT_EndpointId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108042851_UpdateFloorAndAisleStatus'
)
BEGIN
    DROP INDEX [IX_AISLE_EndpointId] ON [AISLE];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108042851_UpdateFloorAndAisleStatus'
)
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AISLE]') AND [c].[name] = N'Code');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [AISLE] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [AISLE] DROP COLUMN [Code];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108042851_UpdateFloorAndAisleStatus'
)
BEGIN
    DECLARE @var1 sysname;
    SELECT @var1 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AISLE]') AND [c].[name] = N'EndpointId');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [AISLE] DROP CONSTRAINT [' + @var1 + '];');
    ALTER TABLE [AISLE] DROP COLUMN [EndpointId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108042851_UpdateFloorAndAisleStatus'
)
BEGIN
    ALTER TABLE [FLOOR] ADD [Description] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108042851_UpdateFloorAndAisleStatus'
)
BEGIN
    ALTER TABLE [FLOOR] ADD [Status] nvarchar(20) NOT NULL DEFAULT N'Active';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108042851_UpdateFloorAndAisleStatus'
)
BEGIN
    ALTER TABLE [AISLE] ADD [Status] nvarchar(20) NOT NULL DEFAULT N'Active';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108042851_UpdateFloorAndAisleStatus'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261108042851_UpdateFloorAndAisleStatus', N'8.0.31');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045333_AlignLogicalErd'
)
BEGIN
    DECLARE @var2 sysname;
    SELECT @var2 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SHELF]') AND [c].[name] = N'Code');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [SHELF] DROP CONSTRAINT [' + @var2 + '];');
    ALTER TABLE [SHELF] DROP COLUMN [Code];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045333_AlignLogicalErd'
)
BEGIN
    EXEC sp_rename N'[BOOK].[Image]', N'ImageUrl', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045333_AlignLogicalErd'
)
BEGIN
    ALTER TABLE [ZONE] ADD [Description] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045333_AlignLogicalErd'
)
BEGIN
    ALTER TABLE [ZONE] ADD [Name] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045333_AlignLogicalErd'
)
BEGIN
    ALTER TABLE [WORKFLOW_TASK] ADD [Description] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045333_AlignLogicalErd'
)
BEGIN
    ALTER TABLE [WORKFLOW] ADD [CreatedAtUtc] datetime2 NOT NULL DEFAULT (GETUTCDATE());
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045333_AlignLogicalErd'
)
BEGIN
    ALTER TABLE [WORKFLOW] ADD [Status] nvarchar(max) NOT NULL DEFAULT N'Active';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045333_AlignLogicalErd'
)
BEGIN
    ALTER TABLE [TASK_STEP] ADD [SequenceNumber] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045333_AlignLogicalErd'
)
BEGIN
    ALTER TABLE [SHELF_LEVEL] ADD [Name] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045333_AlignLogicalErd'
)
BEGIN
    ALTER TABLE [SHELF_LEVEL] ADD [Status] nvarchar(max) NOT NULL DEFAULT N'Active';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045333_AlignLogicalErd'
)
BEGIN
    ALTER TABLE [SHELF] ADD [Description] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045333_AlignLogicalErd'
)
BEGIN
    ALTER TABLE [SHELF] ADD [EndpointId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045333_AlignLogicalErd'
)
BEGIN
    ALTER TABLE [SHELF] ADD [Status] nvarchar(32) NOT NULL DEFAULT N'Active';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045333_AlignLogicalErd'
)
BEGIN
    ALTER TABLE [SCHEDULE] ADD [Data] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045333_AlignLogicalErd'
)
BEGIN
    ALTER TABLE [SCHEDULE] ADD [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045333_AlignLogicalErd'
)
BEGIN
    ALTER TABLE [SCHEDULE] ADD [Name] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045333_AlignLogicalErd'
)
BEGIN
    ALTER TABLE [SCHEDULE] ADD [WorkflowId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045333_AlignLogicalErd'
)
BEGIN
    ALTER TABLE [ROBOT_LOG] ADD [Data] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045333_AlignLogicalErd'
)
BEGIN
    ALTER TABLE [ROBOT_LOG] ADD [MapId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045333_AlignLogicalErd'
)
BEGIN
    ALTER TABLE [ROBOT_LOG] ADD [PositionX] float NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045333_AlignLogicalErd'
)
BEGIN
    ALTER TABLE [ROBOT_LOG] ADD [PositionY] float NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045333_AlignLogicalErd'
)
BEGIN
    ALTER TABLE [ROBOT_LOG] ADD [Status] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045333_AlignLogicalErd'
)
BEGIN
    ALTER TABLE [ROBOT] ADD [CreatedAtUtc] datetime2 NOT NULL DEFAULT (GETUTCDATE());
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045333_AlignLogicalErd'
)
BEGIN
    ALTER TABLE [ROBOT] ADD [MapId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045333_AlignLogicalErd'
)
BEGIN
    ALTER TABLE [ROBOT] ADD [UpdatedAtUtc] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045333_AlignLogicalErd'
)
BEGIN
    ALTER TABLE [PATROL_REPORT] ADD [CompletedAtUtc] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045333_AlignLogicalErd'
)
BEGIN
    ALTER TABLE [PATROL_REPORT] ADD [Data] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045333_AlignLogicalErd'
)
BEGIN
    ALTER TABLE [PATROL_REPORT] ADD [JobId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045333_AlignLogicalErd'
)
BEGIN
    ALTER TABLE [PATROL_REPORT] ADD [StartedAtUtc] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045333_AlignLogicalErd'
)
BEGIN
    ALTER TABLE [MAP] ADD [FloorId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045333_AlignLogicalErd'
)
BEGIN
    ALTER TABLE [MAP] ADD [Status] nvarchar(max) NOT NULL DEFAULT N'Active';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045333_AlignLogicalErd'
)
BEGIN
    ALTER TABLE [JOB_TASK] ADD [Description] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045333_AlignLogicalErd'
)
BEGIN
    ALTER TABLE [JOB_TASK] ADD [Name] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045333_AlignLogicalErd'
)
BEGIN
    ALTER TABLE [JOB_TASK] ADD [Status] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045333_AlignLogicalErd'
)
BEGIN
    ALTER TABLE [JOB_TASK] ADD [WorkflowTaskId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045333_AlignLogicalErd'
)
BEGIN
    ALTER TABLE [JOB_STEP] ADD [Description] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045333_AlignLogicalErd'
)
BEGIN
    ALTER TABLE [JOB_STEP] ADD [Status] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045333_AlignLogicalErd'
)
BEGIN
    ALTER TABLE [JOB_STEP] ADD [StepType] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045333_AlignLogicalErd'
)
BEGIN
    ALTER TABLE [JOB] ADD [RobotId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045333_AlignLogicalErd'
)
BEGIN
    ALTER TABLE [INVENTORY_REPORT] ADD [Data] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045333_AlignLogicalErd'
)
BEGIN
    ALTER TABLE [INVENTORY_REPORT] ADD [FloorId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045333_AlignLogicalErd'
)
BEGIN
    ALTER TABLE [INVENTORY_REPORT] ADD [JobId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045333_AlignLogicalErd'
)
BEGIN
    ALTER TABLE [ENDPOINT_GROUP] ADD [Description] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045333_AlignLogicalErd'
)
BEGIN
    ALTER TABLE [ENDPOINT] ADD [Description] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045333_AlignLogicalErd'
)
BEGIN
    ALTER TABLE [ENDPOINT] ADD [EndpointType] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045333_AlignLogicalErd'
)
BEGIN
    ALTER TABLE [BOOK] ADD [Description] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045333_AlignLogicalErd'
)
BEGIN
    ALTER TABLE [ACCOUNT] ADD [RoleId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045333_AlignLogicalErd'
)
BEGIN
    ALTER TABLE [ACCOUNT] ADD [UpdatedAtUtc] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045333_AlignLogicalErd'
)
BEGIN
    CREATE INDEX [IX_SHELF_EndpointId] ON [SHELF] ([EndpointId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045333_AlignLogicalErd'
)
BEGIN
    CREATE INDEX [IX_PATROL_REPORT_JobId] ON [PATROL_REPORT] ([JobId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045333_AlignLogicalErd'
)
BEGIN
    CREATE INDEX [IX_MAP_FloorId] ON [MAP] ([FloorId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045333_AlignLogicalErd'
)
BEGIN
    CREATE INDEX [IX_JOB_RobotId] ON [JOB] ([RobotId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045333_AlignLogicalErd'
)
BEGIN
    CREATE INDEX [IX_INVENTORY_REPORT_FloorId] ON [INVENTORY_REPORT] ([FloorId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045333_AlignLogicalErd'
)
BEGIN
    CREATE INDEX [IX_INVENTORY_REPORT_JobId] ON [INVENTORY_REPORT] ([JobId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045333_AlignLogicalErd'
)
BEGIN
    ALTER TABLE [INVENTORY_REPORT] ADD CONSTRAINT [FK_INVENTORY_REPORT_FLOOR_FloorId] FOREIGN KEY ([FloorId]) REFERENCES [FLOOR] ([Id]) ON DELETE SET NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045333_AlignLogicalErd'
)
BEGIN
    ALTER TABLE [INVENTORY_REPORT] ADD CONSTRAINT [FK_INVENTORY_REPORT_JOB_JobId] FOREIGN KEY ([JobId]) REFERENCES [JOB] ([Id]) ON DELETE SET NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045333_AlignLogicalErd'
)
BEGIN
    ALTER TABLE [JOB] ADD CONSTRAINT [FK_JOB_ROBOT_RobotId] FOREIGN KEY ([RobotId]) REFERENCES [ROBOT] ([Id]) ON DELETE SET NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045333_AlignLogicalErd'
)
BEGIN
    ALTER TABLE [MAP] ADD CONSTRAINT [FK_MAP_FLOOR_FloorId] FOREIGN KEY ([FloorId]) REFERENCES [FLOOR] ([Id]) ON DELETE SET NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045333_AlignLogicalErd'
)
BEGIN
    ALTER TABLE [PATROL_REPORT] ADD CONSTRAINT [FK_PATROL_REPORT_JOB_JobId] FOREIGN KEY ([JobId]) REFERENCES [JOB] ([Id]) ON DELETE SET NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045333_AlignLogicalErd'
)
BEGIN
    ALTER TABLE [SHELF] ADD CONSTRAINT [FK_SHELF_ENDPOINT_EndpointId] FOREIGN KEY ([EndpointId]) REFERENCES [ENDPOINT] ([Id]) ON DELETE SET NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045333_AlignLogicalErd'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261108045333_AlignLogicalErd', N'8.0.31');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045839_AddErdRelationships'
)
BEGIN
    CREATE INDEX [IX_SCHEDULE_WorkflowId] ON [SCHEDULE] ([WorkflowId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045839_AddErdRelationships'
)
BEGIN
    CREATE INDEX [IX_ROBOT_LOG_MapId] ON [ROBOT_LOG] ([MapId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045839_AddErdRelationships'
)
BEGIN
    CREATE INDEX [IX_ROBOT_MapId] ON [ROBOT] ([MapId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045839_AddErdRelationships'
)
BEGIN
    CREATE INDEX [IX_JOB_TASK_WorkflowTaskId] ON [JOB_TASK] ([WorkflowTaskId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045839_AddErdRelationships'
)
BEGIN
    ALTER TABLE [JOB_TASK] ADD CONSTRAINT [FK_JOB_TASK_WORKFLOW_TASK_WorkflowTaskId] FOREIGN KEY ([WorkflowTaskId]) REFERENCES [WORKFLOW_TASK] ([Id]) ON DELETE SET NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045839_AddErdRelationships'
)
BEGIN
    ALTER TABLE [ROBOT] ADD CONSTRAINT [FK_ROBOT_MAP_MapId] FOREIGN KEY ([MapId]) REFERENCES [MAP] ([Id]) ON DELETE SET NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045839_AddErdRelationships'
)
BEGIN
    ALTER TABLE [ROBOT_LOG] ADD CONSTRAINT [FK_ROBOT_LOG_MAP_MapId] FOREIGN KEY ([MapId]) REFERENCES [MAP] ([Id]) ON DELETE SET NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045839_AddErdRelationships'
)
BEGIN
    ALTER TABLE [SCHEDULE] ADD CONSTRAINT [FK_SCHEDULE_WORKFLOW_WorkflowId] FOREIGN KEY ([WorkflowId]) REFERENCES [WORKFLOW] ([Id]) ON DELETE SET NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108045839_AddErdRelationships'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261108045839_AddErdRelationships', N'8.0.31');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108053054_AddEntityStatuses'
)
BEGIN
    DECLARE @var3 sysname;
    SELECT @var3 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[WORKFLOW]') AND [c].[name] = N'Status');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [WORKFLOW] DROP CONSTRAINT [' + @var3 + '];');
    ALTER TABLE [WORKFLOW] ALTER COLUMN [Status] nvarchar(20) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108053054_AddEntityStatuses'
)
BEGIN
    DECLARE @var4 sysname;
    SELECT @var4 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SHELF_LEVEL]') AND [c].[name] = N'Status');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [SHELF_LEVEL] DROP CONSTRAINT [' + @var4 + '];');
    ALTER TABLE [SHELF_LEVEL] ALTER COLUMN [Status] nvarchar(20) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108053054_AddEntityStatuses'
)
BEGIN
    DECLARE @var5 sysname;
    SELECT @var5 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SHELF]') AND [c].[name] = N'Status');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [SHELF] DROP CONSTRAINT [' + @var5 + '];');
    ALTER TABLE [SHELF] ALTER COLUMN [Status] nvarchar(20) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108053054_AddEntityStatuses'
)
BEGIN
    DECLARE @var6 sysname;
    SELECT @var6 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MAP]') AND [c].[name] = N'Status');
    IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [MAP] DROP CONSTRAINT [' + @var6 + '];');
    ALTER TABLE [MAP] ALTER COLUMN [Status] nvarchar(20) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108053054_AddEntityStatuses'
)
BEGIN
    DECLARE @var7 sysname;
    SELECT @var7 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[BOOK_ITEM]') AND [c].[name] = N'Status');
    IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [BOOK_ITEM] DROP CONSTRAINT [' + @var7 + '];');
    ALTER TABLE [BOOK_ITEM] ALTER COLUMN [Status] nvarchar(20) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108053054_AddEntityStatuses'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261108053054_AddEntityStatuses', N'8.0.31');
END;
GO

COMMIT;
GO

