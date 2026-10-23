using LiBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiBot.Infrastructure.Persistence.Migrations;

[DbContext(typeof(LiBotDbContext))]
[Migration("20261014010000_AddEndpointThetaAndDirectGuidance")]
public partial class AddEndpointThetaAndDirectGuidance : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<double>(
            name: "Theta",
            table: "ENDPOINT",
            type: "float",
            nullable: false,
            defaultValue: 0d);

        migrationBuilder.Sql("""
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
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "Theta", table: "ENDPOINT");
    }
}
