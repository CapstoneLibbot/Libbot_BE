using LiBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiBot.Infrastructure.Persistence.Migrations;

[DbContext(typeof(LiBotDbContext))]
[Migration("20261028110000_SeedFacilityGuidanceWorkflow")]
public partial class SeedFacilityGuidanceWorkflow : Migration
{
    private static readonly Guid SeedWorkflowId = Guid.Parse("e9f15a59-d9b4-4ad9-b2e0-5d8aa6841a42");

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DECLARE @SeedWorkflowId uniqueidentifier = (
                SELECT TOP (1) Id
                FROM WORKFLOW
                WHERE Type = 'SEARCH_AND_GUIDE_TO_FACILITY' AND IsActive = 1
                ORDER BY Id);
            DECLARE @SeedNewWorkflow bit = 0;

            IF @SeedWorkflowId IS NULL
            BEGIN
                SET @SeedWorkflowId = 'e9f15a59-d9b4-4ad9-b2e0-5d8aa6841a42';
                SET @SeedNewWorkflow = 1;
                INSERT INTO WORKFLOW (Id, Name, Description, Type, IsActive)
                VALUES (
                    @SeedWorkflowId,
                    N'Tìm và hướng dẫn đến facility',
                    N'Tìm địa điểm, hiển thị vị trí, điều hướng người đọc và lưu đánh giá.',
                    'SEARCH_AND_GUIDE_TO_FACILITY',
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
                (1, 'F10', 'PROCESS_DATA', N'{"action":"SEARCH_FACILITY","payload":{"query":"${query}","match_mode":"FUZZY","runtime_id":"${runtime_id}"},"output_mapping":{"data.facilities":"facility_results"},"transitions":{"on_success":"F20","on_error":"F210"}}'),
                (2, 'F20', 'DISPLAY', N'{"ui_screen":"FACILITY_SEARCH_RESULTS","data":{"facilities":"${facility_results}"},"transitions":{"on_success":"F30","on_error":"F210"}}'),
                (3, 'F30', 'WAIT', N'{"wait_for":"USER_TOUCH","event_type":"FACILITY_SELECTED","result_variable":"selected_facility_id","timeout_sec":120,"transitions":{"on_event":"F40","on_timeout":"F210","on_cancel":"F210"}}'),
                (4, 'F40', 'PROCESS_DATA', N'{"action":"GET_FACILITY_DETAIL","payload":{"facility_id":"${selected_facility_id}","runtime_id":"${runtime_id}"},"output_mapping":{"data":"facility_detail"},"transitions":{"on_success":"F50","on_error":"F210"}}'),
                (5, 'F50', 'DISPLAY', N'{"ui_screen":"FACILITY_INFO","data":"${facility_detail}","transitions":{"on_success":"F60","on_error":"F210"}}'),
                (6, 'F60', 'WAIT', N'{"wait_for":"USER_TOUCH","event_type":"FACILITY_ACTION","allowed_values":["VIEW_LOCATION","START_GUIDANCE","RATE"],"result_variable":"facility_action","timeout_sec":120,"transitions":{"on_event":"F70","on_timeout":"F160","on_cancel":"F160"}}'),
                (7, 'F70', 'EVALUATE', N'{"condition_type":"DATA_MATCH","left":"${facility_action}","operator":"EQUAL","right":"RATE","transitions":{"on_true":"F160","on_false":"F75"}}'),
                (8, 'F75', 'EVALUATE', N'{"condition_type":"DATA_MATCH","left":"${facility_action}","operator":"EQUAL","right":"START_GUIDANCE","transitions":{"on_true":"F110","on_false":"F80"}}'),
                (9, 'F80', 'DISPLAY', N'{"ui_screen":"MAP_LOCATION","data":"${facility_detail.location}","transitions":{"on_success":"F90","on_error":"F210"}}'),
                (10, 'F90', 'WAIT', N'{"wait_for":"USER_TOUCH","event_type":"MAP_ACTION","allowed_values":["START_GUIDANCE","RATE"],"result_variable":"map_action","timeout_sec":120,"transitions":{"on_event":"F100","on_timeout":"F160","on_cancel":"F160"}}'),
                (11, 'F100', 'EVALUATE', N'{"condition_type":"DATA_MATCH","left":"${map_action}","operator":"EQUAL","right":"RATE","transitions":{"on_true":"F160","on_false":"F110"}}'),
                (12, 'F110', 'DISPLAY', N'{"ui_screen":"NAV_GUIDE","data":{"destination":"${facility_detail.location}","show_route":true},"transitions":{"on_success":"F120","on_error":"F210"}}'),
                (13, 'F120', 'MOVE', N'{"target":"${facility_detail.location.endpoint}","speed_mode":"SLOW_GUIDE","obstacle_policy":"WAIT_RESUME","timeout_sec":180,"output_mapping":{"move_status":"move_status","final_pose":"current_pose"},"transitions":{"on_success":"F130","on_error":"F210","on_timeout":"F210"}}'),
                (14, 'F130', 'DISPLAY', N'{"ui_screen":"ARRIVED","data":"${facility_detail.location}","transitions":{"on_success":"F140","on_error":"F210"}}'),
                (15, 'F140', 'NOTIFY', N'{"channel":"SPEAKER_TTS","message_type":"INFO","content":"Da den khu vuc can tim. Vui long xem huong dan tren man hinh.","transitions":{"on_success":"F150","on_error":"F210"}}'),
                (16, 'F150', 'WAIT', N'{"wait_for":"TIMEOUT","timeout_sec":15,"transitions":{"on_timeout":"F160","on_success":"F160"}}'),
                (17, 'F160', 'DISPLAY', N'{"ui_screen":"RATING","data":{"prompt":"Danh gia trai nghiem cua ban"},"transitions":{"on_success":"F170","on_error":"F210"}}'),
                (18, 'F170', 'WAIT', N'{"wait_for":"USER_TOUCH","event_type":"RATING_SUBMIT","result_variable":"rating","timeout_sec":10,"transitions":{"on_event":"F180","on_timeout":"F190","on_cancel":"F190"}}'),
                (19, 'F180', 'PROCESS_DATA', N'{"action":"SAVE_FACILITY_RATING","payload":{"rating":"${rating}","task_id":"${task_id}","runtime_id":"${runtime_id}"},"transitions":{"on_success":"F190","on_error":"F210"}}'),
                (20, 'F190', 'DISPLAY', N'{"ui_screen":"HOME","data":{},"transitions":{"on_success":"F200","on_error":"F210"}}'),
                (21, 'F200', 'END', N'{"result_status":"SUCCEEDED"}'),
                (22, 'F210', 'END', N'{"result_status":"FAILED"}');

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
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql($"DELETE FROM WORKFLOW WHERE Id = '{SeedWorkflowId:D}';");
    }
}
