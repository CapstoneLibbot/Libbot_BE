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

    public static readonly IReadOnlyList<WorkflowTemplateResponse> All =
    [
        new("search-guide-book", "Tìm và hướng dẫn tới sách", "Tìm sách, nhận lựa chọn và dẫn người đọc đến kệ.", "SEARCH_AND_GUIDE_TO_BOOK", new[]
        {
            MakeStep(1, "S10", "PROCESS_DATA", new() { ["action"] = "SEARCH_BOOK", ["payload"] = new { query = "${query}", match_mode = "FUZZY", runtime_id = "${runtime_id}" }, ["output_mapping"] = new Dictionary<string, string> { ["data.books"] = "search_results" }, ["transitions"] = new { on_success = "S20", on_error = "S210" } }),
            MakeStep(2, "S20", "DISPLAY", new() { ["ui_screen"] = "BOOK_SEARCH_RESULTS", ["data"] = new { books = "${search_results}" }, ["transitions"] = new { on_success = "S30", on_error = "S210" } }),
            MakeStep(3, "S30", "WAIT", new() { ["wait_for"] = "USER_TOUCH", ["event_type"] = "BOOK_SELECTED", ["result_variable"] = "selected_book_id", ["timeout_sec"] = 120, ["transitions"] = new { on_event = "S40", on_timeout = "S210", on_cancel = "S210" } }),
            MakeStep(4, "S40", "PROCESS_DATA", new() { ["action"] = "GET_BOOK_DETAIL", ["payload"] = new { book_id = "${selected_book_id}", runtime_id = "${runtime_id}" }, ["output_mapping"] = new { data = "book_detail" }, ["transitions"] = new { on_success = "S50", on_error = "S210" } }),
            MakeStep(5, "S50", "DISPLAY", new() { ["ui_screen"] = "BOOK_INFO", ["data"] = "${book_detail}", ["transitions"] = new { on_success = "S60", on_error = "S210" } }),
            MakeStep(6, "S60", "WAIT", new() { ["wait_for"] = "USER_TOUCH", ["event_type"] = "BOOK_ACTION", ["allowed_values"] = new[] { "VIEW_LOCATION", "START_GUIDANCE", "RATE" }, ["result_variable"] = "book_action", ["timeout_sec"] = 120, ["transitions"] = new { on_event = "S70", on_timeout = "S160", on_cancel = "S160" } }),
            MakeStep(7, "S70", "EVALUATE", new() { ["condition_type"] = "DATA_MATCH", ["left"] = "${book_action}", ["operator"] = "EQUAL", ["right"] = "RATE", ["transitions"] = new { on_true = "S160", on_false = "S75" } }),
            MakeStep(8, "S75", "EVALUATE", new() { ["condition_type"] = "DATA_MATCH", ["left"] = "${book_action}", ["operator"] = "EQUAL", ["right"] = "START_GUIDANCE", ["transitions"] = new { on_true = "S110", on_false = "S80" } }),
            MakeStep(9, "S80", "DISPLAY", new() { ["ui_screen"] = "MAP_LOCATION", ["data"] = "${book_detail.location}", ["transitions"] = new { on_success = "S90", on_error = "S210" } }),
            MakeStep(10, "S90", "WAIT", new() { ["wait_for"] = "USER_TOUCH", ["event_type"] = "MAP_ACTION", ["allowed_values"] = new[] { "START_GUIDANCE", "RATE" }, ["result_variable"] = "map_action", ["timeout_sec"] = 120, ["transitions"] = new { on_event = "S100", on_timeout = "S160", on_cancel = "S160" } }),
            MakeStep(11, "S100", "EVALUATE", new() { ["condition_type"] = "DATA_MATCH", ["left"] = "${map_action}", ["operator"] = "EQUAL", ["right"] = "RATE", ["transitions"] = new { on_true = "S160", on_false = "S110" } }),
            MakeStep(12, "S110", "DISPLAY", new() { ["ui_screen"] = "NAV_GUIDE", ["data"] = new { destination = "${book_detail.location}", show_route = true }, ["transitions"] = new { on_success = "S120", on_error = "S210" } }),
            MakeStep(13, "S120", "MOVE", new() { ["target"] = "${book_detail.location.endpoint}", ["speed_mode"] = "SLOW_GUIDE", ["obstacle_policy"] = "WAIT_RESUME", ["timeout_sec"] = 180, ["output_mapping"] = new { move_status = "move_status", final_pose = "current_pose" }, ["transitions"] = new { on_success = "S130", on_error = "S210", on_timeout = "S210" } }),
            MakeStep(14, "S130", "DISPLAY", new() { ["ui_screen"] = "ARRIVED", ["data"] = "${book_detail.location}", ["transitions"] = new { on_success = "S140", on_error = "S210" } }),
            MakeStep(15, "S140", "NOTIFY", new() { ["channel"] = "SPEAKER_TTS", ["message_type"] = "INFO", ["content"] = "Đã đến vị trí sách. Vui lòng xem kệ và tầng kệ hiển thị trên màn hình.", ["transitions"] = new { on_success = "S150", on_error = "S210" } }),
            MakeStep(16, "S150", "WAIT", new() { ["wait_for"] = "TIMEOUT", ["timeout_sec"] = 15, ["transitions"] = new { on_timeout = "S160", on_success = "S160" } }),
            MakeStep(17, "S160", "DISPLAY", new() { ["ui_screen"] = "RATING", ["data"] = new { prompt = "Đánh giá trải nghiệm của bạn" }, ["transitions"] = new { on_success = "S170", on_error = "S210" } }),
            MakeStep(18, "S170", "WAIT", new() { ["wait_for"] = "USER_TOUCH", ["event_type"] = "RATING_SUBMIT", ["result_variable"] = "rating", ["timeout_sec"] = 10, ["transitions"] = new { on_event = "S180", on_timeout = "S190", on_cancel = "S190" } }),
            MakeStep(19, "S180", "PROCESS_DATA", new() { ["action"] = "SAVE_RATING", ["payload"] = new { rating = "${rating}", task_id = "${task_id}", runtime_id = "${runtime_id}" }, ["transitions"] = new { on_success = "S190", on_error = "S210" } }),
            MakeStep(20, "S190", "DISPLAY", new() { ["ui_screen"] = "HOME", ["data"] = new { }, ["transitions"] = new { on_success = "S200", on_error = "S210" } }),
            MakeStep(21, "S200", "END", new() { ["result_status"] = "SUCCEEDED" }),
            MakeStep(22, "S210", "END", new() { ["result_status"] = "FAILED" })
        }),
        new("search-guide-facility", "Tìm và hướng dẫn tới tiện ích", "Tìm tiện ích trong thư viện và điều hướng tới đó.", "SEARCH_AND_GUIDE_TO_FACILITY", new[]
        {
            MakeStep(1, "F10", "PROCESS_DATA", new() { ["action"] = "SEARCH_FACILITY", ["payload"] = new { query = "${query}", runtime_id = "${runtime_id}" }, ["output_mapping"] = new { data = "search_results" }, ["transitions"] = new { on_success = "F20", on_error = "F90" } }),
            MakeStep(2, "F20", "DISPLAY", new() { ["ui_screen"] = "FACILITY_SEARCH_RESULTS", ["data"] = "${search_results}", ["transitions"] = new { on_success = "F30", on_error = "F90" } }),
            MakeStep(3, "F30", "WAIT", new() { ["wait_for"] = "USER_TOUCH", ["event_type"] = "FACILITY_SELECTED", ["result_variable"] = "selected_facility_id", ["timeout_sec"] = 120, ["transitions"] = new { on_event = "F40", on_timeout = "F90", on_cancel = "F90" } }),
            MakeStep(4, "F40", "PROCESS_DATA", new() { ["action"] = "GET_FACILITY_DETAIL", ["payload"] = new { facility_id = "${selected_facility_id}", runtime_id = "${runtime_id}" }, ["output_mapping"] = new { data = "facility_detail" }, ["transitions"] = new { on_success = "F50", on_error = "F90" } }),
            MakeStep(5, "F50", "DISPLAY", new() { ["ui_screen"] = "FACILITY_INFO", ["data"] = "${facility_detail}", ["transitions"] = new { on_success = "F60", on_error = "F90" } }),
            MakeStep(6, "F60", "MOVE", new() { ["target"] = "${facility_detail.location.endpoint}", ["timeout_sec"] = 180, ["transitions"] = new { on_success = "F70", on_error = "F90", on_timeout = "F90" } }),
            MakeStep(7, "F70", "DISPLAY", new() { ["ui_screen"] = "ARRIVED", ["data"] = "${facility_detail.location}", ["transitions"] = new { on_success = "F80", on_error = "F90" } }),
            MakeStep(8, "F80", "END", new() { ["status"] = "SUCCEEDED" }),
            MakeStep(9, "F90", "END", new() { ["status"] = "FAILED" })
        }),
        new("bookshelf-inventory", "Kiểm kê kệ sách", "Quét nhiều barcode tại kệ và lập báo cáo kiểm kê.", "BOOKSHELF_INVENTORY", new[]
        {
            MakeStep(1, "I10", "MOVE", new() { ["endpoint_name"] = "Kệ A3", ["transitions"] = new { on_success = "I20", on_error = "I80" } }),
            MakeStep(2, "I20", "SCAN", new() { ["sensor"] = "camera", ["device"] = "imx500", ["output_key"] = "scanned_code", ["timeout_sec"] = 15, ["transitions"] = new { on_success = "I30", on_timeout = "I50", on_error = "I80" } }),
            MakeStep(3, "I30", "PROCESS_DATA", new() { ["action"] = "RECORD_INVENTORY", ["payload"] = new { barcode = "${scanned_code}", shelf_code = "A3", mission_id = "${mission_id}", runtime_id = "${runtime_id}" }, ["output_mapping"] = new { data = "last_inventory_scan" }, ["transitions"] = new { on_success = "I40", on_error = "I80" } }),
            MakeStep(4, "I40", "WAIT", new() { ["wait_for"] = "USER_TOUCH", ["event_type"] = "INVENTORY_ACTION", ["allowed_values"] = new[] { "CONTINUE", "DONE" }, ["result_variable"] = "inventory_action", ["timeout_sec"] = 300, ["transitions"] = new { on_event = "I45", on_timeout = "I80" } }),
            MakeStep(5, "I45", "EVALUATE", new() { ["left"] = "${inventory_action}", ["operator"] = "EQUAL", ["right"] = "DONE", ["transitions"] = new { on_true = "I50", on_false = "I20" } }),
            MakeStep(6, "I50", "PROCESS_DATA", new() { ["action"] = "FINALIZE_INVENTORY", ["payload"] = new { mission_id = "${mission_id}", runtime_id = "${runtime_id}", shelf_code = "A3" }, ["transitions"] = new { on_success = "I60", on_error = "I80" } }),
            MakeStep(7, "I60", "NOTIFY", new() { ["channel"] = "SPEAKER_TTS", ["content"] = "Inventory scan completed.", ["transitions"] = new { on_success = "I70", on_error = "I80" } }),
            MakeStep(8, "I70", "END", new() { ["status"] = "SUCCEEDED" }),
            MakeStep(9, "I80", "END", new() { ["status"] = "FAILED" })
        }),
        new("library-patrol", "Tuần tra thư viện", "Đi tới điểm tuần tra, ghi nhận quan sát và tạo báo cáo.", "LIBRARY_PATROL", new[]
        {
            MakeStep(1, "P10", "MOVE", new() { ["endpoint_name"] = "Kệ A3", ["transitions"] = new { on_success = "P20", on_error = "P70" } }),
            MakeStep(2, "P20", "SCAN", new() { ["sensor"] = "patrol_observation", ["context_key"] = "patrol_observation", ["output_key"] = "patrol_observation", ["timeout_sec"] = 10, ["transitions"] = new { on_success = "P30", on_timeout = "P25", on_error = "P70" } }),
            MakeStep(3, "P25", "WAIT", new() { ["wait_for"] = "USER_TOUCH", ["event_type"] = "PATROL_OBSERVATION", ["allowed_values"] = new[] { "FALLEN_BOOK", "OBSTACLE", "WARNING" }, ["result_variable"] = "patrol_observation", ["prompt"] = "Select the observed library issue", ["timeout_sec"] = 120, ["transitions"] = new { on_event = "P30", on_timeout = "P70" } }),
            MakeStep(4, "P30", "PROCESS_DATA", new() { ["action"] = "RECORD_PATROL_OBSERVATION", ["payload"] = new { observation = "${patrol_observation}", mission_id = "${mission_id}", runtime_id = "${runtime_id}" }, ["transitions"] = new { on_success = "P40", on_error = "P70" } }),
            MakeStep(5, "P40", "DISPLAY", new() { ["ui_screen"] = "PATROL_OBSERVATION", ["data"] = "${patrol_observation}", ["transitions"] = new { on_success = "P50" } }),
            MakeStep(6, "P50", "PROCESS_DATA", new() { ["action"] = "GENERATE_PATROL_REPORT", ["payload"] = new { mission_id = "${mission_id}", runtime_id = "${runtime_id}" }, ["transitions"] = new { on_success = "P60", on_error = "P70" } }),
            MakeStep(7, "P60", "NOTIFY", new() { ["channel"] = "LIBRARIAN_WS", ["event_type"] = "PATROL_COMPLETED", ["content"] = "Library patrol completed.", ["transitions"] = new { on_success = "P80", on_error = "P70" } }),
            MakeStep(8, "P70", "END", new() { ["status"] = "FAILED" }),
            MakeStep(9, "P80", "END", new() { ["status"] = "SUCCEEDED" })
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
}

