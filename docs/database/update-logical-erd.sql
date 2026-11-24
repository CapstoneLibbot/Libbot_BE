-- LiBot Logical ERD - Microsoft SQL Server DDL
-- Generated from SEP490-Logical ERD (1).json
-- The source ERD does not specify SQL data types; types/defaults/constraints are inferred.
-- All primary keys and foreign-key columns use UNIQUEIDENTIFIER (GUID).
-- Primary keys default to NEWSEQUENTIALID() to reduce index fragmentation.
-- JSON fields use NVARCHAR(MAX), because SQL Server stores JSON as text.
-- Naming normalizations: BOOK_CATEROGY -> book_category; ENPOINT_GROUP -> endpoint_group;
-- ENPOINT_GROUP_MEMBER -> endpoint_group_member; NOTIFICATION.titile -> title.
-- The source spelling enpoint_group_id is retained for the endpoint-group FK column.

BEGIN TRANSACTION;

CREATE TABLE [account] (
    [id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    [username] VARCHAR(100) NOT NULL UNIQUE,
    [password_hash] VARCHAR(255) NOT NULL,
    [role] VARCHAR(50) NOT NULL,
    [full_name] VARCHAR(150),
    [email] VARCHAR(255) UNIQUE,
    [phone] VARCHAR(30),
    [status] VARCHAR(20) NOT NULL DEFAULT 'active',
    [created_at] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    [updated_at] DATETIME2(7),
    [last_login_at] DATETIME2(7)
);

CREATE TABLE [audit_log] (
    [id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    [account_id] UNIQUEIDENTIFIER,
    [action] VARCHAR(100) NOT NULL,
    [entity_type] VARCHAR(100) NOT NULL,
    [entity_id] UNIQUEIDENTIFIER,
    [old_value] NVARCHAR(MAX),
    [new_value] NVARCHAR(MAX),
    [created_at] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE TABLE [notification] (
    [id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    [account_id] UNIQUEIDENTIFIER NOT NULL,
    [type] VARCHAR(50) NOT NULL,
    [title] VARCHAR(255) NOT NULL,
    [message] NVARCHAR(MAX) NOT NULL,
    [severity] VARCHAR(30),
    [is_read] BIT NOT NULL DEFAULT 0,
    [created_at] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    [read_at] DATETIME2(7)
);

CREATE TABLE [book_item] (
    [id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    [expected_shelf_level_id] UNIQUEIDENTIFIER,
    [current_shelf_level_id] UNIQUEIDENTIFIER,
    [book_id] UNIQUEIDENTIFIER NOT NULL,
    [barcode] VARCHAR(100) UNIQUE,
    [last_inventory_at] DATETIME2(7),
    [status] VARCHAR(30) NOT NULL
);

CREATE TABLE [aisle] (
    [id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    [floor_id] UNIQUEIDENTIFIER NOT NULL,
    [name] VARCHAR(100) NOT NULL,
    [description] NVARCHAR(MAX),
    [status] VARCHAR(30)
);

CREATE TABLE [book] (
    [id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    [category_id] UNIQUEIDENTIFIER,
    [isbn] VARCHAR(20) UNIQUE,
    [title] VARCHAR(500) NOT NULL,
    [author] VARCHAR(255),
    [image_url] NVARCHAR(MAX),
    [description] NVARCHAR(MAX),
    [publisher] VARCHAR(255),
    [publication_year] SMALLINT
);

CREATE TABLE [floor] (
    [id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    [name] VARCHAR(100) NOT NULL,
    [floor_number] INTEGER NOT NULL UNIQUE,
    [description] NVARCHAR(MAX),
    [status] VARCHAR(30)
);

CREATE TABLE [shelf_level] (
    [id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    [shelf_id] UNIQUEIDENTIFIER NOT NULL,
    [name] VARCHAR(100),
    [level_number] INTEGER NOT NULL,
    [status] VARCHAR(30),
    CONSTRAINT [uq_shelf_level_number] UNIQUE ([shelf_id], [level_number])
);

CREATE TABLE [zone] (
    [id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    [map_id] UNIQUEIDENTIFIER NOT NULL,
    [name] VARCHAR(100) NOT NULL,
    [zone_type] VARCHAR(50),
    [created_at] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    [max_speed] NUMERIC(8,3),
    [description] NVARCHAR(MAX),
    [min_x] NUMERIC(12,4),
    [min_y] NUMERIC(12,4),
    [max_x] NUMERIC(12,4),
    [max_y] NUMERIC(12,4),
    [is_active] BIT NOT NULL DEFAULT 1
);

CREATE TABLE [shelf] (
    [id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    [start_endpoint_id] UNIQUEIDENTIFIER,
    [end_endpoint_id] UNIQUEIDENTIFIER,
    [aisle_id] UNIQUEIDENTIFIER NOT NULL,
    [name] VARCHAR(100) NOT NULL,
    [description] NVARCHAR(MAX),
    [status] VARCHAR(30)
);

CREATE TABLE [book_category] (
    [id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    [name] VARCHAR(150) NOT NULL UNIQUE,
    [description] NVARCHAR(MAX)
);

CREATE TABLE [endpoint] (
    [id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    [map_id] UNIQUEIDENTIFIER NOT NULL,
    [name] VARCHAR(100) NOT NULL,
    [description] NVARCHAR(MAX),
    [endpoint_type] VARCHAR(50),
    [position_x] NUMERIC(12,4) NOT NULL,
    [position_y] NUMERIC(12,4) NOT NULL,
    [is_active] BIT NOT NULL DEFAULT 1,
    [theta] NUMERIC(10,6)
);

CREATE TABLE [feedback] (
    [id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    [job_id] UNIQUEIDENTIFIER NOT NULL,
    [rating] SMALLINT NOT NULL,
    [comment] NVARCHAR(MAX)
);

CREATE TABLE [job_event] (
    [id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    [job_step_id] UNIQUEIDENTIFIER NOT NULL,
    [event_time] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    [data] NVARCHAR(MAX)
);

CREATE TABLE [robot] (
    [id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    [map_id] UNIQUEIDENTIFIER,
    [name] VARCHAR(100) NOT NULL,
    [serial_number] VARCHAR(100) NOT NULL UNIQUE,
    [status] VARCHAR(30) NOT NULL,
    [battery_level] NUMERIC(5,2),
    [create_at] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    [update_at] DATETIME2(7)
);

CREATE TABLE [workflow] (
    [id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    [name] VARCHAR(150) NOT NULL,
    [type] VARCHAR(50) NOT NULL,
    [description] NVARCHAR(MAX),
    [status] VARCHAR(30) NOT NULL,
    [create_at] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE TABLE [workflow_task] (
    [id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    [workflow_id] UNIQUEIDENTIFIER NOT NULL,
    [name] VARCHAR(150) NOT NULL,
    [description] NVARCHAR(MAX),
    [sequence_no] INTEGER NOT NULL,
    [created_at] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [uq_workflow_task_sequence] UNIQUE ([workflow_id], [sequence_no])
);

CREATE TABLE [task_step] (
    [id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    [workflow_task_id] UNIQUEIDENTIFIER NOT NULL,
    [sequence_no] INTEGER NOT NULL,
    [step_type] VARCHAR(50) NOT NULL,
    [configuration] NVARCHAR(MAX),
    [created_at] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    [description] NVARCHAR(MAX),
    CONSTRAINT [uq_task_step_sequence] UNIQUE ([workflow_task_id], [sequence_no])
);

CREATE TABLE [robot_log] (
    [id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    [robot_id] UNIQUEIDENTIFIER NOT NULL,
    [position_x] NUMERIC(12,4),
    [map_id] UNIQUEIDENTIFIER,
    [position_y] NUMERIC(12,4),
    [current_battery_level] NUMERIC(5,2),
    [status] VARCHAR(30),
    [data] NVARCHAR(MAX),
    [record_at] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE TABLE [job_task] (
    [id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    [job_id] UNIQUEIDENTIFIER NOT NULL,
    [workflow_task_id] UNIQUEIDENTIFIER NOT NULL,
    [name] VARCHAR(150) NOT NULL,
    [sequence_no] INTEGER NOT NULL,
    [status] VARCHAR(30) NOT NULL,
    CONSTRAINT [uq_job_task_sequence] UNIQUE ([job_id], [sequence_no])
);

CREATE TABLE [map] (
    [id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    [floor_id] UNIQUEIDENTIFIER NOT NULL,
    [name] VARCHAR(150) NOT NULL,
    [version] INTEGER NOT NULL DEFAULT 1,
    [width] INTEGER,
    [height] INTEGER,
    [map_data] NVARCHAR(MAX),
    [status] VARCHAR(30)
);

CREATE TABLE [job] (
    [id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    [workflow_id] UNIQUEIDENTIFIER NOT NULL,
    [robot_id] UNIQUEIDENTIFIER,
    [name] VARCHAR(150) NOT NULL,
    [priority] INTEGER NOT NULL DEFAULT 0,
    [status] VARCHAR(30) NOT NULL,
    [description] NVARCHAR(MAX),
    [started_at] DATETIME2(7),
    [completed_at] DATETIME2(7),
    [create_at] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE TABLE [inventory_report] (
    [id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    [job_id] UNIQUEIDENTIFIER NOT NULL,
    [scope_type] VARCHAR(50) NOT NULL,
    [total_shelfs] INTEGER NOT NULL DEFAULT 0,
    [total_book_scaned] INTEGER NOT NULL DEFAULT 0,
    [total_available] INTEGER NOT NULL DEFAULT 0,
    [total_misplaced] INTEGER NOT NULL DEFAULT 0,
    [total_missing] INTEGER NOT NULL DEFAULT 0,
    [data] NVARCHAR(MAX),
    [created_at] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE TABLE [endpoint_group_member] (
    [id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    [enpoint_group_id] UNIQUEIDENTIFIER NOT NULL,
    [endpoint_id] UNIQUEIDENTIFIER NOT NULL,
    CONSTRAINT [uq_endpoint_group_member] UNIQUE ([enpoint_group_id], [endpoint_id])
);

CREATE TABLE [endpoint_group] (
    [id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    [name] VARCHAR(150) NOT NULL,
    [description] NVARCHAR(MAX)
);

CREATE TABLE [patrol_report] (
    [id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    [job_id] UNIQUEIDENTIFIER NOT NULL,
    [started_at] DATETIME2(7),
    [completed_at] DATETIME2(7),
    [duration_sec] INTEGER,
    [observation_count] INTEGER NOT NULL DEFAULT 0,
    [data] NVARCHAR(MAX),
    [warning_count] INTEGER NOT NULL DEFAULT 0,
    [created_at] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE TABLE [job_step] (
    [id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    [job_task_id] UNIQUEIDENTIFIER NOT NULL,
    [task_step_id] UNIQUEIDENTIFIER NOT NULL,
    [step_type] VARCHAR(50) NOT NULL,
    [sequence] INTEGER NOT NULL,
    [status] VARCHAR(30) NOT NULL,
    [configuration] NVARCHAR(MAX),
    CONSTRAINT [uq_job_step_sequence] UNIQUE ([job_task_id], [sequence])
);

CREATE TABLE [schedule] (
    [id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    [workflow_id] UNIQUEIDENTIFIER NOT NULL,
    [schedule_type] VARCHAR(50) NOT NULL,
    [name] VARCHAR(150),
    [data] NVARCHAR(MAX),
    [schedule_time] TIME,
    [start_date] DATE,
    [end_date] DATE,
    [is_active] BIT NOT NULL DEFAULT 1,
    [last_run_at] DATETIME2(7),
    [next_run_at] DATETIME2(7),
    [created_at] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    [updated_at] DATETIME2(7)
);

CREATE TABLE [inventory_report_detail] (
    [id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    [report_id] UNIQUEIDENTIFIER NOT NULL,
    [book_item_id] UNIQUEIDENTIFIER NOT NULL,
    [expected_shelf_level_id] UNIQUEIDENTIFIER,
    [detected_shelf_level_id] UNIQUEIDENTIFIER,
    [status] VARCHAR(30) NOT NULL,
    [result] NVARCHAR(MAX)
);

CREATE TABLE [book_issue_detail] (
    [id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    [issue_id] UNIQUEIDENTIFIER NOT NULL,
    [book_item_id] UNIQUEIDENTIFIER NOT NULL,
    [note] NVARCHAR(MAX)
);

CREATE TABLE [book_issue] (
    [id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    [issue_type] VARCHAR(50) NOT NULL,
    [issue_date] DATETIME2(7) NOT NULL,
    [status] VARCHAR(30) NOT NULL,
    [total] INTEGER NOT NULL DEFAULT 0,
    [note] NVARCHAR(MAX)
);

CREATE TABLE [config_battery_robot] (
    [id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    [account_id] UNIQUEIDENTIFIER NOT NULL,
    [robot_id] UNIQUEIDENTIFIER NOT NULL,
    [battery_threshold] NUMERIC(5,2) NOT NULL,
    [is_active] BIT NOT NULL DEFAULT 1
);

-- Foreign keys (added after all tables exist)
ALTER TABLE [audit_log] ADD CONSTRAINT [fk_audit_log_account_id] FOREIGN KEY ([account_id]) REFERENCES [account] ([id]) ON UPDATE CASCADE ON DELETE NO ACTION;
ALTER TABLE [notification] ADD CONSTRAINT [fk_notification_account_id] FOREIGN KEY ([account_id]) REFERENCES [account] ([id]) ON UPDATE CASCADE ON DELETE NO ACTION;
ALTER TABLE [book_item] ADD CONSTRAINT [fk_book_item_expected_shelf_level_id] FOREIGN KEY ([expected_shelf_level_id]) REFERENCES [shelf_level] ([id]) ON UPDATE CASCADE ON DELETE NO ACTION;
ALTER TABLE [book_item] ADD CONSTRAINT [fk_book_item_current_shelf_level_id] FOREIGN KEY ([current_shelf_level_id]) REFERENCES [shelf_level] ([id]) ON UPDATE CASCADE ON DELETE NO ACTION;
ALTER TABLE [book_item] ADD CONSTRAINT [fk_book_item_book_id] FOREIGN KEY ([book_id]) REFERENCES [book] ([id]) ON UPDATE CASCADE ON DELETE NO ACTION;
ALTER TABLE [aisle] ADD CONSTRAINT [fk_aisle_floor_id] FOREIGN KEY ([floor_id]) REFERENCES [floor] ([id]) ON UPDATE CASCADE ON DELETE NO ACTION;
ALTER TABLE [book] ADD CONSTRAINT [fk_book_category_id] FOREIGN KEY ([category_id]) REFERENCES [book_category] ([id]) ON UPDATE CASCADE ON DELETE NO ACTION;
ALTER TABLE [shelf_level] ADD CONSTRAINT [fk_shelf_level_shelf_id] FOREIGN KEY ([shelf_id]) REFERENCES [shelf] ([id]) ON UPDATE CASCADE ON DELETE NO ACTION;
ALTER TABLE [zone] ADD CONSTRAINT [fk_zone_map_id] FOREIGN KEY ([map_id]) REFERENCES [map] ([id]) ON UPDATE CASCADE ON DELETE NO ACTION;
ALTER TABLE [shelf] ADD CONSTRAINT [fk_shelf_start_endpoint_id] FOREIGN KEY ([start_endpoint_id]) REFERENCES [endpoint] ([id]) ON UPDATE CASCADE ON DELETE NO ACTION;
ALTER TABLE [shelf] ADD CONSTRAINT [fk_shelf_end_endpoint_id] FOREIGN KEY ([end_endpoint_id]) REFERENCES [endpoint] ([id]) ON UPDATE CASCADE ON DELETE NO ACTION;
ALTER TABLE [shelf] ADD CONSTRAINT [fk_shelf_aisle_id] FOREIGN KEY ([aisle_id]) REFERENCES [aisle] ([id]) ON UPDATE CASCADE ON DELETE NO ACTION;
ALTER TABLE [endpoint] ADD CONSTRAINT [fk_endpoint_map_id] FOREIGN KEY ([map_id]) REFERENCES [map] ([id]) ON UPDATE CASCADE ON DELETE NO ACTION;
ALTER TABLE [feedback] ADD CONSTRAINT [fk_feedback_job_id] FOREIGN KEY ([job_id]) REFERENCES [job] ([id]) ON UPDATE CASCADE ON DELETE NO ACTION;
ALTER TABLE [job_event] ADD CONSTRAINT [fk_job_event_job_step_id] FOREIGN KEY ([job_step_id]) REFERENCES [job_step] ([id]) ON UPDATE CASCADE ON DELETE NO ACTION;
ALTER TABLE [robot] ADD CONSTRAINT [fk_robot_map_id] FOREIGN KEY ([map_id]) REFERENCES [map] ([id]) ON UPDATE CASCADE ON DELETE NO ACTION;
ALTER TABLE [workflow_task] ADD CONSTRAINT [fk_workflow_task_workflow_id] FOREIGN KEY ([workflow_id]) REFERENCES [workflow] ([id]) ON UPDATE CASCADE ON DELETE NO ACTION;
ALTER TABLE [task_step] ADD CONSTRAINT [fk_task_step_workflow_task_id] FOREIGN KEY ([workflow_task_id]) REFERENCES [workflow_task] ([id]) ON UPDATE CASCADE ON DELETE NO ACTION;
ALTER TABLE [robot_log] ADD CONSTRAINT [fk_robot_log_robot_id] FOREIGN KEY ([robot_id]) REFERENCES [robot] ([id]) ON UPDATE CASCADE ON DELETE NO ACTION;
ALTER TABLE [robot_log] ADD CONSTRAINT [fk_robot_log_map_id] FOREIGN KEY ([map_id]) REFERENCES [map] ([id]) ON UPDATE CASCADE ON DELETE NO ACTION;
ALTER TABLE [job_task] ADD CONSTRAINT [fk_job_task_job_id] FOREIGN KEY ([job_id]) REFERENCES [job] ([id]) ON UPDATE CASCADE ON DELETE NO ACTION;
ALTER TABLE [job_task] ADD CONSTRAINT [fk_job_task_workflow_task_id] FOREIGN KEY ([workflow_task_id]) REFERENCES [workflow_task] ([id]) ON UPDATE CASCADE ON DELETE NO ACTION;
ALTER TABLE [map] ADD CONSTRAINT [fk_map_floor_id] FOREIGN KEY ([floor_id]) REFERENCES [floor] ([id]) ON UPDATE CASCADE ON DELETE NO ACTION;
ALTER TABLE [job] ADD CONSTRAINT [fk_job_workflow_id] FOREIGN KEY ([workflow_id]) REFERENCES [workflow] ([id]) ON UPDATE CASCADE ON DELETE NO ACTION;
ALTER TABLE [job] ADD CONSTRAINT [fk_job_robot_id] FOREIGN KEY ([robot_id]) REFERENCES [robot] ([id]) ON UPDATE CASCADE ON DELETE NO ACTION;
ALTER TABLE [inventory_report] ADD CONSTRAINT [fk_inventory_report_job_id] FOREIGN KEY ([job_id]) REFERENCES [job] ([id]) ON UPDATE CASCADE ON DELETE NO ACTION;
ALTER TABLE [endpoint_group_member] ADD CONSTRAINT [fk_endpoint_group_member_enpoint_group_id] FOREIGN KEY ([enpoint_group_id]) REFERENCES [endpoint_group] ([id]) ON UPDATE CASCADE ON DELETE NO ACTION;
ALTER TABLE [endpoint_group_member] ADD CONSTRAINT [fk_endpoint_group_member_endpoint_id] FOREIGN KEY ([endpoint_id]) REFERENCES [endpoint] ([id]) ON UPDATE CASCADE ON DELETE NO ACTION;
ALTER TABLE [patrol_report] ADD CONSTRAINT [fk_patrol_report_job_id] FOREIGN KEY ([job_id]) REFERENCES [job] ([id]) ON UPDATE CASCADE ON DELETE NO ACTION;
ALTER TABLE [job_step] ADD CONSTRAINT [fk_job_step_job_task_id] FOREIGN KEY ([job_task_id]) REFERENCES [job_task] ([id]) ON UPDATE CASCADE ON DELETE NO ACTION;
ALTER TABLE [job_step] ADD CONSTRAINT [fk_job_step_task_step_id] FOREIGN KEY ([task_step_id]) REFERENCES [task_step] ([id]) ON UPDATE CASCADE ON DELETE NO ACTION;
ALTER TABLE [schedule] ADD CONSTRAINT [fk_schedule_workflow_id] FOREIGN KEY ([workflow_id]) REFERENCES [workflow] ([id]) ON UPDATE CASCADE ON DELETE NO ACTION;
ALTER TABLE [inventory_report_detail] ADD CONSTRAINT [fk_inventory_report_detail_report_id] FOREIGN KEY ([report_id]) REFERENCES [inventory_report] ([id]) ON UPDATE CASCADE ON DELETE NO ACTION;
ALTER TABLE [inventory_report_detail] ADD CONSTRAINT [fk_inventory_report_detail_book_item_id] FOREIGN KEY ([book_item_id]) REFERENCES [book_item] ([id]) ON UPDATE CASCADE ON DELETE NO ACTION;
ALTER TABLE [inventory_report_detail] ADD CONSTRAINT [fk_inventory_report_detail_expected_shelf_level_id] FOREIGN KEY ([expected_shelf_level_id]) REFERENCES [shelf_level] ([id]) ON UPDATE CASCADE ON DELETE NO ACTION;
ALTER TABLE [inventory_report_detail] ADD CONSTRAINT [fk_inventory_report_detail_detected_shelf_level_id] FOREIGN KEY ([detected_shelf_level_id]) REFERENCES [shelf_level] ([id]) ON UPDATE CASCADE ON DELETE NO ACTION;
ALTER TABLE [book_issue_detail] ADD CONSTRAINT [fk_book_issue_detail_issue_id] FOREIGN KEY ([issue_id]) REFERENCES [book_issue] ([id]) ON UPDATE CASCADE ON DELETE NO ACTION;
ALTER TABLE [book_issue_detail] ADD CONSTRAINT [fk_book_issue_detail_book_item_id] FOREIGN KEY ([book_item_id]) REFERENCES [book_item] ([id]) ON UPDATE CASCADE ON DELETE NO ACTION;
ALTER TABLE [config_battery_robot] ADD CONSTRAINT [fk_config_battery_robot_account_id] FOREIGN KEY ([account_id]) REFERENCES [account] ([id]) ON UPDATE CASCADE ON DELETE NO ACTION;
ALTER TABLE [config_battery_robot] ADD CONSTRAINT [fk_config_battery_robot_robot_id] FOREIGN KEY ([robot_id]) REFERENCES [robot] ([id]) ON UPDATE CASCADE ON DELETE NO ACTION;

-- Indexes for foreign keys
CREATE INDEX [ix_audit_log_account_id] ON [audit_log] ([account_id]);
CREATE INDEX [ix_notification_account_id] ON [notification] ([account_id]);
CREATE INDEX [ix_book_item_expected_shelf_level_id] ON [book_item] ([expected_shelf_level_id]);
CREATE INDEX [ix_book_item_current_shelf_level_id] ON [book_item] ([current_shelf_level_id]);
CREATE INDEX [ix_book_item_book_id] ON [book_item] ([book_id]);
CREATE INDEX [ix_aisle_floor_id] ON [aisle] ([floor_id]);
CREATE INDEX [ix_book_category_id] ON [book] ([category_id]);
CREATE INDEX [ix_shelf_level_shelf_id] ON [shelf_level] ([shelf_id]);
CREATE INDEX [ix_zone_map_id] ON [zone] ([map_id]);
CREATE INDEX [ix_shelf_start_endpoint_id] ON [shelf] ([start_endpoint_id]);
CREATE INDEX [ix_shelf_end_endpoint_id] ON [shelf] ([end_endpoint_id]);
CREATE INDEX [ix_shelf_aisle_id] ON [shelf] ([aisle_id]);
CREATE INDEX [ix_endpoint_map_id] ON [endpoint] ([map_id]);
CREATE INDEX [ix_feedback_job_id] ON [feedback] ([job_id]);
CREATE INDEX [ix_job_event_job_step_id] ON [job_event] ([job_step_id]);
CREATE INDEX [ix_robot_map_id] ON [robot] ([map_id]);
CREATE INDEX [ix_workflow_task_workflow_id] ON [workflow_task] ([workflow_id]);
CREATE INDEX [ix_task_step_workflow_task_id] ON [task_step] ([workflow_task_id]);
CREATE INDEX [ix_robot_log_robot_id] ON [robot_log] ([robot_id]);
CREATE INDEX [ix_robot_log_map_id] ON [robot_log] ([map_id]);
CREATE INDEX [ix_job_task_job_id] ON [job_task] ([job_id]);
CREATE INDEX [ix_job_task_workflow_task_id] ON [job_task] ([workflow_task_id]);
CREATE INDEX [ix_map_floor_id] ON [map] ([floor_id]);
CREATE INDEX [ix_job_workflow_id] ON [job] ([workflow_id]);
CREATE INDEX [ix_job_robot_id] ON [job] ([robot_id]);
CREATE INDEX [ix_inventory_report_job_id] ON [inventory_report] ([job_id]);
CREATE INDEX [ix_endpoint_group_member_enpoint_group_id] ON [endpoint_group_member] ([enpoint_group_id]);
CREATE INDEX [ix_endpoint_group_member_endpoint_id] ON [endpoint_group_member] ([endpoint_id]);
CREATE INDEX [ix_patrol_report_job_id] ON [patrol_report] ([job_id]);
CREATE INDEX [ix_job_step_job_task_id] ON [job_step] ([job_task_id]);
CREATE INDEX [ix_job_step_task_step_id] ON [job_step] ([task_step_id]);
CREATE INDEX [ix_schedule_workflow_id] ON [schedule] ([workflow_id]);
CREATE INDEX [ix_inventory_report_detail_report_id] ON [inventory_report_detail] ([report_id]);
CREATE INDEX [ix_inventory_report_detail_book_item_id] ON [inventory_report_detail] ([book_item_id]);
CREATE INDEX [ix_inventory_report_detail_expected_shelf_level_id] ON [inventory_report_detail] ([expected_shelf_level_id]);
CREATE INDEX [ix_inventory_report_detail_detected_shelf_level_id] ON [inventory_report_detail] ([detected_shelf_level_id]);
CREATE INDEX [ix_book_issue_detail_issue_id] ON [book_issue_detail] ([issue_id]);
CREATE INDEX [ix_book_issue_detail_book_item_id] ON [book_issue_detail] ([book_item_id]);
CREATE INDEX [ix_config_battery_robot_account_id] ON [config_battery_robot] ([account_id]);
CREATE INDEX [ix_config_battery_robot_robot_id] ON [config_battery_robot] ([robot_id]);

-- CHECK constraints
ALTER TABLE [feedback] ADD CONSTRAINT [ck_feedback_rating] CHECK ([rating] BETWEEN 1 AND 5);
ALTER TABLE [robot] ADD CONSTRAINT [ck_robot_battery_level] CHECK ([battery_level] IS NULL OR [battery_level] BETWEEN 0 AND 100);
ALTER TABLE [robot_log] ADD CONSTRAINT [ck_robot_log_battery] CHECK ([current_battery_level] IS NULL OR [current_battery_level] BETWEEN 0 AND 100);
ALTER TABLE [config_battery_robot] ADD CONSTRAINT [ck_config_battery_threshold] CHECK ([battery_threshold] BETWEEN 0 AND 100);
ALTER TABLE [book_issue] ADD CONSTRAINT [ck_book_issue_total] CHECK ([total] >= 0);
ALTER TABLE [inventory_report] ADD CONSTRAINT [ck_inventory_report_counts] CHECK ([total_shelfs] >= 0 AND [total_book_scaned] >= 0 AND [total_available] >= 0 AND [total_misplaced] >= 0 AND [total_missing] >= 0);

COMMIT TRANSACTION;
