using LiBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiBot.Infrastructure.Persistence.Migrations;

[Migration("20261125010000_NormalizeJobTaskStepSnapshots")]
[DbContext(typeof(LiBotDbContext))]
public sealed class NormalizeJobTaskStepSnapshots : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
IF COL_LENGTH(N'dbo.JOB_TASK', N'runtime_id') IS NULL
BEGIN
    ALTER TABLE dbo.JOB_TASK ADD [runtime_id] nvarchar(16) NOT NULL
        CONSTRAINT [df_job_task_runtime_id] DEFAULT N'real';
END;

IF COL_LENGTH(N'dbo.JOB_TASK', N'execution_id') IS NULL
    ALTER TABLE dbo.JOB_TASK ADD [execution_id] nvarchar(128) NULL;

IF COL_LENGTH(N'dbo.JOB_STEP', N'configuration') IS NULL
    ALTER TABLE dbo.JOB_STEP ADD [configuration] nvarchar(max) NULL;
""");

        migrationBuilder.Sql("""
-- Recover runtime IDs from the old per-task state events before new SIM runs stop creating them.
;WITH RuntimeEvents AS
(
    SELECT task.id AS JobTaskId,
           COALESCE(JSON_VALUE(jobEvent.data, '$.data.runtimeId'),
                    JSON_VALUE(JSON_VALUE(jobEvent.data, '$.data'), '$.runtimeId')) AS RuntimeId,
           COALESCE(JSON_VALUE(jobEvent.data, '$.data.executionId'),
                    JSON_VALUE(JSON_VALUE(jobEvent.data, '$.data'), '$.executionId')) AS ExecutionId,
           ROW_NUMBER() OVER (PARTITION BY task.id ORDER BY jobEvent.event_time DESC, jobEvent.id DESC) AS RowNumber
    FROM dbo.JOB_TASK AS task
    JOIN dbo.JOB_STEP AS step ON step.job_task_id = task.id
    JOIN dbo.JOB_EVENT AS jobEvent ON jobEvent.job_step_id = step.id
    WHERE JSON_VALUE(jobEvent.data, '$.event_type') = N'__JOB_TASK_STATE'
      AND COALESCE(JSON_VALUE(jobEvent.data, '$.data.runtimeId'),
                   JSON_VALUE(JSON_VALUE(jobEvent.data, '$.data'), '$.runtimeId')) IS NOT NULL
)
UPDATE task
   SET runtime_id = LOWER(events.RuntimeId), execution_id = events.ExecutionId
FROM dbo.JOB_TASK AS task
JOIN RuntimeEvents AS events ON events.JobTaskId = task.id AND events.RowNumber = 1;

-- Each job step gets an immutable JSON snapshot with the robot-facing id, name, and type fields.
;WITH StepSnapshots AS
(
    SELECT jobStep.id,
           CASE WHEN ISJSON(jobStep.configuration) = 1 AND LEFT(LTRIM(jobStep.configuration), 1) = N'{'
                THEN jobStep.configuration ELSE N'{}' END AS ExistingConfiguration,
           CASE WHEN ISJSON(taskStep.configuration) = 1 AND LEFT(LTRIM(taskStep.configuration), 1) = N'{'
                THEN taskStep.configuration ELSE N'{}' END AS WorkflowConfiguration,
           taskStep.id AS TaskStepId,
           taskStep.step_type AS StepType,
           workflowTask.name AS TaskName
    FROM dbo.JOB_STEP AS jobStep
    JOIN dbo.TASK_STEP AS taskStep ON taskStep.id = jobStep.task_step_id
    JOIN dbo.WORKFLOW_TASK AS workflowTask ON workflowTask.id = taskStep.workflow_task_id
)
UPDATE jobStep
   SET configuration = JSON_MODIFY(
       JSON_MODIFY(
           JSON_MODIFY(
               snapshots.ExistingConfiguration,
               '$.id', COALESCE(NULLIF(JSON_VALUE(snapshots.ExistingConfiguration, '$.id'), N''),
                                NULLIF(JSON_VALUE(snapshots.WorkflowConfiguration, '$.id'), N''), CONVERT(nvarchar(36), snapshots.TaskStepId))),
           '$.name', COALESCE(NULLIF(JSON_VALUE(snapshots.ExistingConfiguration, '$.name'), N''),
                              NULLIF(JSON_VALUE(snapshots.WorkflowConfiguration, '$.name'), N''), snapshots.TaskName)),
       '$.type', snapshots.StepType)
FROM dbo.JOB_STEP AS jobStep
JOIN StepSnapshots AS snapshots ON snapshots.Id = jobStep.id;

ALTER TABLE dbo.JOB_STEP ALTER COLUMN configuration nvarchar(max) NOT NULL;

-- Expand legacy jobs that stored all workflow steps under one job task into one job task per workflow task.
UPDATE dbo.JOB_TASK SET sequence_no = sequence_no + 1000000;

;WITH JobWorkflowTasks AS
(
    SELECT DISTINCT jobTask.job_id AS JobId,
           workflowTask.id AS WorkflowTaskId,
           workflowTask.sequence_no AS WorkflowSequence,
           workflowTask.name AS WorkflowTaskName
    FROM dbo.JOB_STEP AS jobStep
    JOIN dbo.JOB_TASK AS jobTask ON jobTask.id = jobStep.job_task_id
    JOIN dbo.TASK_STEP AS taskStep ON taskStep.id = jobStep.task_step_id
    JOIN dbo.WORKFLOW_TASK AS workflowTask ON workflowTask.id = taskStep.workflow_task_id
),
MissingJobTasks AS
(
    SELECT groups.JobId, groups.WorkflowTaskId, groups.WorkflowSequence, groups.WorkflowTaskName,
           sourceTask.Status, sourceTask.runtime_id
    FROM JobWorkflowTasks AS groups
    CROSS APPLY
    (
        SELECT TOP (1) task.status, task.runtime_id
        FROM dbo.JOB_TASK AS task
        WHERE task.job_id = groups.JobId
        ORDER BY task.sequence_no, task.id
    ) AS sourceTask
    WHERE NOT EXISTS
    (
        SELECT 1 FROM dbo.JOB_TASK AS task
        WHERE task.job_id = groups.JobId AND task.workflow_task_id = groups.WorkflowTaskId
    )
)
INSERT INTO dbo.JOB_TASK (id, job_id, workflow_task_id, name, status, sequence_no, runtime_id)
SELECT NEWID(), JobId, WorkflowTaskId, WorkflowTaskName, Status, WorkflowSequence, runtime_id
FROM MissingJobTasks;

UPDATE jobStep
   SET job_task_id = targetTask.id
FROM dbo.JOB_STEP AS jobStep
JOIN dbo.JOB_TASK AS currentTask ON currentTask.id = jobStep.job_task_id
JOIN dbo.TASK_STEP AS taskStep ON taskStep.id = jobStep.task_step_id
CROSS APPLY
(
    SELECT TOP (1) task.id
    FROM dbo.JOB_TASK AS task
    WHERE task.job_id = currentTask.job_id
      AND task.workflow_task_id = taskStep.workflow_task_id
    ORDER BY task.sequence_no, task.id
) AS targetTask;

UPDATE jobTask SET name = workflowTask.name
FROM dbo.JOB_TASK AS jobTask
JOIN dbo.WORKFLOW_TASK AS workflowTask ON workflowTask.id = jobTask.workflow_task_id;

;WITH DuplicateTasks AS
(
    SELECT id,
           ROW_NUMBER() OVER (PARTITION BY job_id, workflow_task_id ORDER BY sequence_no, id) AS RowNumber
    FROM dbo.JOB_TASK
)
DELETE task
FROM dbo.JOB_TASK AS task
JOIN DuplicateTasks AS duplicates ON duplicates.Id = task.id
WHERE duplicates.RowNumber > 1
  AND NOT EXISTS (SELECT 1 FROM dbo.JOB_STEP AS step WHERE step.job_task_id = task.id);

-- Historical SIM events are transient by contract and are removed from persistent event storage.
DELETE jobEvent
FROM dbo.JOB_EVENT AS jobEvent
JOIN dbo.JOB_STEP AS step ON step.id = jobEvent.job_step_id
JOIN dbo.JOB_TASK AS task ON task.id = step.job_task_id
WHERE task.runtime_id = N'sim';

UPDATE dbo.JOB_TASK SET sequence_no = sequence_no + 1000000;
;WITH OrderedTasks AS
(
    SELECT task.id,
           ROW_NUMBER() OVER (PARTITION BY task.job_id ORDER BY workflowTask.sequence_no, task.id) AS SequenceNo
    FROM dbo.JOB_TASK AS task
    JOIN dbo.WORKFLOW_TASK AS workflowTask ON workflowTask.id = task.workflow_task_id
)
UPDATE task SET sequence_no = ordered.SequenceNo
FROM dbo.JOB_TASK AS task
JOIN OrderedTasks AS ordered ON ordered.Id = task.id;

""");
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql("THROW 51052, 'Restore the database backup to reverse job task and step snapshot normalization.', 1;");
}
