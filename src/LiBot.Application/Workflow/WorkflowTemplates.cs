using System.Text.Json;
using LiBot.Application.Models;

namespace LiBot.Application.Workflow;

public static class WorkflowTemplates
{
    private static JsonElement Config(object value) => JsonSerializer.SerializeToElement(value);

    private static WorkflowTemplateStep MakeStep(int sequence, string id, string type, Dictionary<string, object?> config)
    {
        config["id"] = id;
        return new WorkflowTemplateStep(sequence, type, id, Config(config));
    }

    private static WorkflowTemplateStep MakeEnvelopeStep(
        int sequence,
        string id,
        string type,
        string name,
        Dictionary<string, object?> parameters,
        object? outputMapping,
        object transitions,
        object? policy = null)
    {
        var envelope = new Dictionary<string, object?>
        {
            ["id"] = id,
            ["type"] = type,
            ["name"] = name,
            ["params"] = parameters,
            ["output_mapping"] = outputMapping ?? new Dictionary<string, string>(),
            ["transitions"] = transitions,
            ["policy"] = policy ?? new { max_attempts = 1 }
        };
        return new WorkflowTemplateStep(sequence, type, name, Config(envelope));
    }

    public static readonly IReadOnlyList<WorkflowTemplateResponse> All =
    [
        CreateReturnAndChargingTemplate(),
        new("search-guide-book", "Tìm và hướng dẫn tới sách", "Dẫn reader tới kệ của bản sách đã chọn và thông báo vị trí.", "SEARCH_AND_GUIDE_TO_BOOK", new[]
        {
            MakeStep(1, "S10", "PROCESS_DATA", new()
            {
                ["action"] = "GET_BOOK_DETAIL",
                ["payload"] = new { book_id = "${book_id}", book_item_id = "${book_item_id}", runtime_id = "${runtime_id}" },
                ["output_mapping"] = new { data = "book_detail" },
                ["transitions"] = new { on_success = "S20" }
            }),
            MakeStep(2, "S20", "MOVE", new()
            {
                ["target"] = "${book_detail.location.endpoint}",
                ["speed_mode"] = "SLOW_GUIDE",
                ["obstacle_policy"] = "WAIT_RESUME",
                ["timeout_sec"] = 180,
                ["output_mapping"] = new { move_status = "move_status", final_pose = "current_pose" },
                ["transitions"] = new { on_success = "S30" }
            }),
            MakeStep(3, "S30", "WAIT", new()
            {
                ["wait_for"] = "DURATION",
                ["seconds"] = 2,
                ["timeout_sec"] = 2,
                ["transitions"] = new { on_success = "S40" }
            }),
            MakeStep(4, "S40", "NOTIFY", new()
            {
                ["channel"] = "SPEAKER_TTS",
                ["message_type"] = "INFO",
                ["content"] = "Đã đến kệ ${book_detail.location.shelfName}, tầng ${book_detail.location.shelfLevel}. Reader có thể lấy sách ${book_detail.title}."
            })
        }),
        CreateFacilityGuidanceTemplate(),
        new("bookshelf-inventory", "Kiểm kê kệ sách", "Robot quét QR hai tầng mỗi kệ, đối soát trạng thái sách và lập báo cáo.", "BOOKSHELF_INVENTORY", new[]
        {
            MakeEnvelopeStep(1, "I10", "PROCESS_DATA", "RESOLVE_INVENTORY_SCOPE",
                new() { ["action"] = "RESOLVE_INVENTORY_SCOPE", ["payload"] = new { floor_id = "${floor_id}", map_id = "${map_id}", scope_type = "SHELVES", target_shelves = "${target_shelves}" } },
                new Dictionary<string, string> { ["data.shelves"] = "inventory_shelves" },
                new { on_success = "I20", on_error = "I900" }),
            MakeEnvelopeStep(2, "I20", "PROCESS_DATA", "SELECT_FIRST_INVENTORY_SHELF",
                new() { ["action"] = "ADVANCE_INVENTORY_SCOPE", ["payload"] = new { shelves = "${inventory_shelves}", current_shelf_index = -1 } },
                new Dictionary<string, string> { ["data.current_shelf"] = "current_shelf", ["data.current_shelf_index"] = "current_shelf_index", ["data.has_next"] = "has_next" },
                new { on_success = "I30", on_error = "I900" }),
            MakeEnvelopeStep(3, "I30", "EVALUATE", "CHECK_INITIAL_INVENTORY_SHELF",
                new() { ["left"] = "${has_next}", ["operator"] = "EQUAL", ["right"] = true }, null,
                new { on_true = "I40", on_false = "I110", on_error = "I900" }),
            MakeEnvelopeStep(4, "I40", "MOVE", "MOVE_TO_INVENTORY_SHELF",
                new() { ["target"] = "${current_shelf.endpoint}", ["speed_mode"] = "NORMAL", ["obstacle_policy"] = "WAIT_RESUME", ["timeout_sec"] = 180, ["_inventory_shelf_scan"] = true },
                new Dictionary<string, string> { ["move_status"] = "move_status", ["final_pose"] = "current_pose" },
                new { on_success = "I50", on_error = "I900", on_timeout = "I900" },
                new { max_attempts = 1, timeout_sec = 180 }),
            MakeEnvelopeStep(5, "I50", "SCAN", "SCAN_QR_ON_BOTH_SHELF_LEVELS",
                new() { ["scan_type"] = "QR", ["target_type"] = "BOOK", ["timeout_sec"] = 30, ["continuous"] = true },
                new Dictionary<string, string> { ["scanned_items"] = "scanned_items", ["scan_status"] = "scan_status" },
                new { on_success = "I60", on_error = "I900", on_timeout = "I900" },
                new { max_attempts = 1, timeout_sec = 30 }),
            MakeEnvelopeStep(6, "I60", "PROCESS_DATA", "VERIFY_SCANNED_BOOKS",
                new() { ["action"] = "VERIFY_BOOK", ["payload"] = new { shelf_id = "${current_shelf.shelf_id}", scanned_items = "${scanned_items}" } },
                new Dictionary<string, string> { ["data.verified_book_items"] = "verified_book_items", ["data.issue_detected"] = "issue_detected", ["data.verification_status"] = "verification_status" },
                new { on_success = "I70", on_error = "I900" }),
            MakeEnvelopeStep(7, "I70", "EVALUATE", "CHECK_INVENTORY_ISSUES",
                new() { ["condition_type"] = "DATA_MATCH", ["left"] = "${issue_detected}", ["operator"] = "EQUAL", ["right"] = true }, null,
                new { on_true = "I80", on_false = "I90", on_error = "I900" }),
            MakeEnvelopeStep(8, "I80", "PROCESS_DATA", "UPDATE_BOOK_ITEM_INVENTORY_STATUS",
                new() { ["action"] = "UPDATE_BOOK_ITEM_INVENTORY_STATUS", ["payload"] = new { book_items = "${verified_book_items}", update_status = true, update_location = true, aggregation = "TASK" } },
                new Dictionary<string, string> { ["data.updated_book_items"] = "updated_book_items" },
                new { on_success = "I90", on_error = "I900" }),
            MakeEnvelopeStep(9, "I90", "PROCESS_DATA", "ADVANCE_TO_NEXT_INVENTORY_SHELF",
                new() { ["action"] = "ADVANCE_INVENTORY_SCOPE", ["payload"] = new { shelves = "${inventory_shelves}", current_shelf_index = "${current_shelf_index}" } },
                new Dictionary<string, string> { ["data.current_shelf"] = "current_shelf", ["data.current_shelf_index"] = "current_shelf_index", ["data.has_next"] = "has_next" },
                new { on_success = "I100", on_error = "I900" }),
            MakeEnvelopeStep(10, "I100", "EVALUATE", "CHECK_REMAINING_INVENTORY_SHELVES",
                new() { ["left"] = "${has_next}", ["operator"] = "EQUAL", ["right"] = true }, null,
                new { on_true = "I40", on_false = "I110", on_error = "I900" }),
            MakeEnvelopeStep(11, "I110", "PROCESS_DATA", "GENERATE_INVENTORY_REPORT",
                new() { ["action"] = "GENERATE_REPORT", ["payload"] = new { scope_type = "SHELVES", floor_id = "${floor_id}", map_id = "${map_id}", target_shelves = "${target_shelves}", source = "TASK_INVENTORY_RESULTS" } },
                new Dictionary<string, string> { ["data"] = "inventory_report" },
                new { on_success = "I120", on_error = "I900" }),
            MakeEnvelopeStep(12, "I120", "NOTIFY", "NOTIFY_INVENTORY_COMPLETED",
                new() { ["channel"] = "LIBRARIAN_WS", ["message_type"] = "INFO", ["event_type"] = "INVENTORY_COMPLETED", ["content"] = "Kiểm kê hoàn tất", ["payload"] = "${inventory_report}" },
                null,
                new { on_success = "I130", on_error = "I900" }),
            MakeEnvelopeStep(13, "I130", "END", "INVENTORY_SUCCEEDED",
                new() { ["result_status"] = "SUCCEEDED", ["output"] = new { report = "${inventory_report}" } }, null, new { }),
            MakeEnvelopeStep(14, "I900", "NOTIFY", "NOTIFY_INVENTORY_FAILED",
                new() { ["channel"] = "LIBRARIAN_WS", ["message_type"] = "ERROR", ["event_type"] = "INVENTORY_FAILED", ["content"] = "Kiểm kê không hoàn tất. Vui lòng kiểm tra trạng thái robot và cấu hình kệ." },
                null,
                new { on_success = "I910", on_error = "I910" }),
            MakeEnvelopeStep(15, "I910", "END", "INVENTORY_FAILED",
                new() { ["result_status"] = "FAILED", ["message"] = "Inventory workflow failed." }, null, new { })
        }),
        new("library-patrol", "Library Patrol", "Patrol an ordered set of active map endpoints, classify observations, record route coverage and notify librarians.", "LIBRARY_PATROL", new[]
        {
            MakeEnvelopeStep(1, "P10", "PROCESS_DATA", "PATROL_SCOPE",
                new() { ["action"] = "PATROL_SCOPE", ["payload"] = new { mission_id = "${mission_id}", runtime_id = "${runtime_id}", execution_id = "${execution_id}", map_id = "${map_id}", floor_id = "${floor_id}", scope_type = "${scope_type}", target_endpoints = "${target_endpoints}" } },
                new Dictionary<string, string> { ["data.map_id"] = "map_id", ["data.floor_id"] = "floor_id", ["data.scope_type"] = "scope_type", ["data.target_endpoints"] = "target_endpoints" },
                new { on_success = "P20", on_error = "P85" }),
            MakeEnvelopeStep(2, "P20", "PROCESS_DATA", "BUILD_PATROL_COVERAGE_PLAN",
                new() { ["action"] = "BUILD_PATROL_COVERAGE_PLAN", ["payload"] = new { mission_id = "${mission_id}", runtime_id = "${runtime_id}", execution_id = "${execution_id}", map_id = "${map_id}", target_endpoints = "${target_endpoints}", remaining_paths = "${remaining_paths}", current_pose = "${current_pose}" } },
                new Dictionary<string, string> { ["data.current_endpoint"] = "current_endpoint", ["data.next_endpoint"] = "next_endpoint", ["data.current_path"] = "current_path", ["data.remaining_paths"] = "remaining_paths", ["data.remaining_paths_count"] = "remaining_paths_count", ["data.total_paths"] = "total_paths" },
                new { on_success = "P30", on_error = "P85" }),
            MakeEnvelopeStep(3, "P30", "MOVE", "MOVE_TO_PATROL_ENDPOINT",
                new() { ["target"] = "${current_path.target}", ["path_id"] = "${current_path.path_id}", ["speed_mode"] = "PATROL", ["scan_while_moving"] = true, ["obstacle_policy"] = "DETECT_AND_WAIT", ["timeout_sec"] = 180 },
                new Dictionary<string, string> { ["observations"] = "observations", ["actual_distance_m"] = "actual_distance_m", ["move_status"] = "move_status" },
                new { on_success = "P40", on_error = "P85", on_timeout = "P85" },
                new { max_attempts = 1, timeout_sec = 180 }),
            MakeEnvelopeStep(4, "P40", "PROCESS_DATA", "CLASSIFY_PATROL_OBSERVATIONS",
                new() { ["action"] = "CLASSIFY_PATROL_OBSERVATIONS", ["payload"] = new { mission_id = "${mission_id}", runtime_id = "${runtime_id}", execution_id = "${execution_id}", path_id = "${current_path.path_id}", observations = "${observations}" } },
                new Dictionary<string, string> { ["data.events"] = "classified_events", ["data.job_event_id"] = "job_event_id" },
                new { on_success = "P50", on_error = "P85" }),
            MakeEnvelopeStep(5, "P50", "PROCESS_DATA", "UPDATE_PATH_COVERAGE",
                new() { ["action"] = "UPDATE_PATH_COVERAGE", ["payload"] = new { mission_id = "${mission_id}", runtime_id = "${runtime_id}", execution_id = "${execution_id}", job_event_id = "${job_event_id}", planned_path_count = "${total_paths}", path_id = "${current_path.path_id}", status = "COVERED", actual_distance_m = "${actual_distance_m}", remaining_paths = "${remaining_paths}" } },
                new Dictionary<string, string> { ["data.coverage"] = "patrol_coverage", ["data.remaining_paths"] = "remaining_paths", ["data.remaining_paths_count"] = "remaining_paths_count" },
                new { on_success = "P60", on_error = "P85" }),
            MakeEnvelopeStep(6, "P60", "EVALUATE", "CHECK_REMAINING_PATROL_PATHS",
                new() { ["left"] = "${remaining_paths_count}", ["operator"] = "GREATER_THAN", ["right"] = 0 },
                null,
                new { on_true = "P20", on_false = "P70", on_error = "P85" }),
            MakeEnvelopeStep(7, "P70", "PROCESS_DATA", "GENERATE_PATROL_REPORT",
                new() { ["action"] = "GENERATE_PATROL_REPORT", ["payload"] = new { mission_id = "${mission_id}", runtime_id = "${runtime_id}", execution_id = "${execution_id}", map_id = "${map_id}", floor_id = "${floor_id}" } },
                new Dictionary<string, string> { ["data"] = "patrol_report" },
                new { on_success = "P80", on_error = "P85" }),
            MakeEnvelopeStep(8, "P80", "NOTIFY", "NOTIFY_PATROL_COMPLETED",
                new() { ["channel"] = "LIBRARIAN_WS", ["event_type"] = "PATROL_COMPLETED", ["content"] = "Library patrol completed.", ["payload"] = "${patrol_report}" },
                null,
                new { on_success = "P90", on_error = "P85" }),
            MakeEnvelopeStep(9, "P90", "END", "PATROL_SUCCEEDED",
                new() { ["result_status"] = "SUCCEEDED", ["output"] = new { report = "${patrol_report}", coverage = "${patrol_coverage}" } }, null, new { }),
            MakeEnvelopeStep(10, "P85", "NOTIFY", "NOTIFY_PATROL_FAILED",
                new() { ["channel"] = "LIBRARIAN_WS", ["event_type"] = "PATROL_FAILED", ["content"] = "Library patrol failed. Check the mission error and incomplete route." },
                null,
                new { on_success = "P99", on_error = "P99" }),
            MakeEnvelopeStep(11, "P99", "END", "PATROL_FAILED",
                new() { ["result_status"] = "FAILED", ["message"] = "Library patrol did not complete." }, null, new { })
        }),
        new("misplaced-book-detection", "Phát hiện sách sai vị trí", "Quét barcode tại kệ và ghi nhận các bản sao sai kệ.", "MISPLACED_BOOK_DETECTION", new[]
        {
            MakeStep(1, "M10", "MOVE", new() { ["endpoint_name"] = "Kệ A3", ["transitions"] = new { on_success = "M20", on_error = "M80" } }),
            MakeStep(2, "M20", "SCAN", new() { ["sensor"] = "camera", ["device"] = "imx500", ["output_key"] = "scanned_code", ["timeout_sec"] = 15, ["transitions"] = new { on_success = "M30", on_timeout = "M40", on_error = "M80" } }),
            MakeStep(3, "M30", "PROCESS_DATA", new() { ["action"] = "RECORD_INVENTORY", ["payload"] = new { barcode = "${scanned_code}", shelf_code = "A3", mission_id = "${mission_id}", runtime_id = "${runtime_id}" }, ["output_mapping"] = new { data = "last_inventory_scan" }, ["transitions"] = new { on_success = "M32", on_error = "M80" } }),
            MakeStep(4, "M32", "EVALUATE", new() { ["left"] = "${last_inventory_scan.misplaced}", ["operator"] = "EQUAL", ["right"] = true, ["transitions"] = new { on_true = "M35", on_false = "M40" } }),
            MakeStep(5, "M35", "PROCESS_DATA", new() { ["action"] = "ADD_INVENTORY_ISSUE", ["payload"] = new { issue_type = "MISPLACED", barcode = "${last_inventory_scan.barcode}", scanned_at = "${last_inventory_scan.scannedAt}", mission_id = "${mission_id}", runtime_id = "${runtime_id}" }, ["transitions"] = new { on_success = "M40", on_error = "M80" } }),
            MakeStep(6, "M40", "WAIT", new() { ["wait_for"] = "USER_TOUCH", ["event_type"] = "INVENTORY_ACTION", ["allowed_values"] = new[] { "CONTINUE", "DONE" }, ["result_variable"] = "inventory_action", ["timeout_sec"] = 300, ["transitions"] = new { on_event = "M45", on_timeout = "M80" } }),
            MakeStep(7, "M45", "EVALUATE", new() { ["left"] = "${inventory_action}", ["operator"] = "EQUAL", ["right"] = "DONE", ["transitions"] = new { on_true = "M50", on_false = "M20" } }),
            MakeStep(8, "M50", "PROCESS_DATA", new() { ["action"] = "FINALIZE_INVENTORY", ["payload"] = new { mission_id = "${mission_id}", runtime_id = "${runtime_id}", shelf_code = "A3", report_type = "MISPLACED_BOOK_DETECTION" }, ["transitions"] = new { on_success = "M55", on_error = "M80" } }),
            MakeStep(9, "M55", "NOTIFY", new() { ["channel"] = "SPEAKER_TTS", ["content"] = "Misplaced book scan completed.", ["transitions"] = new { on_success = "M70", on_error = "M80" } }),
            MakeStep(10, "M70", "END", new() { ["status"] = "SUCCEEDED" }),
            MakeStep(11, "M80", "END", new() { ["status"] = "FAILED" })
        })
    ];

    private static WorkflowTemplateResponse CreateReturnAndChargingTemplate() =>
        new("robot-return-charging", "Robot Return & Charging",
            "Return the physical robot to the active map charger when the battery is low and wait for librarian confirmation.",
            "ROBOT_RETURN_AND_CHARGING", new[]
            {
                MakeStep(1, "C10", "EVALUATE", new()
                {
                    ["left"] = "${battery_pct}", ["operator"] = "LESS_THAN_OR_EQUAL", ["right"] = 15,
                    ["transitions"] = new { on_true = "C20", on_false = "C90" }
                }),
                MakeStep(2, "C20", "MOVE", new()
                {
                    ["target"] = "${charging_endpoint}", ["speed_mode"] = "SLOW_GUIDE",
                    ["obstacle_policy"] = "WAIT_RESUME", ["timeout_sec"] = 300,
                    ["transitions"] = new { on_success = "C30", on_error = "C95", on_timeout = "C95" }
                }),
                MakeStep(3, "C30", "DISPLAY", new()
                {
                    ["ui_screen"] = "CHARGER_CONNECTION_REQUIRED",
                    ["message"] = "Robot is at the charger. Please connect the charging cable.",
                    ["data"] = new { message = "Robot is at the charger. Please connect the charging cable.", battery_pct = "${battery_pct}", confirmation_required = true },
                    ["transitions"] = new { on_success = "C40", on_error = "C95" }
                }),
                MakeStep(4, "C40", "NOTIFY", new()
                {
                    ["channel"] = "LIBRARIAN_WS", ["event_type"] = "CHARGER_CONNECTION_REQUIRED",
                    ["content"] = "Robot arrived at the charging station. Please connect the charger and confirm in the dashboard.",
                    ["transitions"] = new { on_success = "C50", on_error = "C95" }
                }),
                MakeStep(5, "C50", "WAIT", new()
                {
                    ["wait_for"] = "USER_TOUCH", ["event_type"] = "CHARGER_CONNECTED",
                    ["allowed_values"] = new[] { "CONFIRMED" }, ["result_variable"] = "charger_connection",
                    ["timeout_sec"] = (object?)null, ["show_ui"] = false,
                    ["prompt"] = "Librarian: confirm that the charging cable is connected.",
                    ["transitions"] = new { on_event = "C60", on_cancel = "C95" }
                }),
                MakeStep(6, "C60", "DISPLAY", new()
                {
                    ["ui_screen"] = "CHARGER_CONFIRMED",
                    ["message"] = "Charging connection confirmed by the librarian. Robot will remain at the dock.",
                    ["data"] = new { message = "Charging connection confirmed by the librarian. Robot will remain at the dock.", confirmation_source = "LIBRARIAN" },
                    ["transitions"] = new { on_success = "C70", on_error = "C95" }
                }),
                MakeStep(7, "C70", "NOTIFY", new()
                {
                    ["channel"] = "LIBRARIAN_WS", ["event_type"] = "CHARGER_CONNECTION_CONFIRMED",
                    ["content"] = "Librarian confirmed the charging cable is connected. Robot is waiting at the dock.",
                    ["transitions"] = new { on_success = "C80", on_error = "C95" }
                }),
                MakeStep(8, "C80", "END", new()
                {
                    ["result_status"] = "SUCCEEDED",
                    ["output"] = new { confirmation_source = "LIBRARIAN", charger_connection = "${charger_connection}" }
                }),
                MakeStep(9, "C90", "END", new() { ["result_status"] = "SUCCEEDED", ["message"] = "Battery recovered above the charging threshold." }),
                MakeStep(10, "C95", "END", new() { ["result_status"] = "FAILED" })
            });

    private static WorkflowTemplateResponse CreateFacilityGuidanceTemplate()
    {
        var steps = FacilityGuidanceWorkflow.CreateSteps()
            .Select((step, index) =>
            {
                var type = (string)step["type"]!;
                var name = (string)step["name"]!;
                var configuration = step
                    .Where(item => item.Key != "name" && item.Key != "type")
                    .ToDictionary(item => item.Key, item => item.Value);
                return new WorkflowTemplateStep(
                    index + 1,
                    type,
                    name,
                    JsonSerializer.SerializeToElement(configuration));
            })
            .ToList();

        return new WorkflowTemplateResponse(
            "search-guide-facility",
            "Tìm và hướng dẫn đến facility",
            "Tìm facility trong thư viện, hiển thị vị trí, dẫn người đọc đến nơi và thu thập đánh giá.",
            FacilityGuidanceWorkflow.Type,
            steps);
    }
}

