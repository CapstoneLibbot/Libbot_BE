using System.Text.Json.Serialization;

namespace LiBot.Contracts.Workflow;

public sealed record LiBotStartWorkflowRequest(
    [property: JsonPropertyName("command")] string Command,
    [property: JsonPropertyName("execution_id")] string ExecutionId,
    [property: JsonPropertyName("task_id")] string TaskId,
    [property: JsonPropertyName("initial_context")] IReadOnlyDictionary<string, object?> InitialContext,
    [property: JsonPropertyName("steps")] IReadOnlyList<IReadOnlyDictionary<string, object?>> Steps);

public static class BookGuidanceWorkflow
{
    public const string Type = "SEARCH_AND_GUIDE_TO_BOOK";

    public static IReadOnlyList<IReadOnlyDictionary<string, object?>> CreateSteps() =>
    [
        Step("S10", "PROCESS_DATA", new()
        {
            ["action"] = "SEARCH_BOOK",
            ["payload"] = new Dictionary<string, object?> { ["query"] = "${query}", ["match_mode"] = "FUZZY" },
            ["output_mapping"] = new Dictionary<string, object?> { ["data.books"] = "search_results" },
            ["transitions"] = Success("S20")
        }),
        Step("S20", "DISPLAY", new()
        {
            ["ui_screen"] = "BOOK_SEARCH_RESULTS",
            ["data"] = new Dictionary<string, object?> { ["books"] = "${search_results}" },
            ["transitions"] = Success("S30")
        }),
        Step("S30", "WAIT", new()
        {
            ["wait_for"] = "USER_TOUCH", ["event_type"] = "BOOK_SELECTED",
            ["result_variable"] = "selected_book_id", ["timeout_sec"] = 20,
            ["transitions"] = Event("S40", "S190")
        }),
        Step("S40", "PROCESS_DATA", new()
        {
            ["action"] = "GET_BOOK_DETAIL",
            ["payload"] = new Dictionary<string, object?> { ["book_id"] = "${selected_book_id}" },
            ["output_mapping"] = new Dictionary<string, object?> { ["data"] = "book_detail" },
            ["transitions"] = Success("S50")
        }),
        Step("S50", "DISPLAY", new()
        {
            ["ui_screen"] = "BOOK_INFO", ["data"] = "${book_detail}",
            ["transitions"] = Success("S60")
        }),
        Step("S60", "WAIT", new()
        {
            ["wait_for"] = "USER_TOUCH", ["event_type"] = "BOOK_ACTION",
            ["allowed_values"] = new[] { "VIEW_LOCATION", "RATE" }, ["result_variable"] = "book_action",
            ["timeout_sec"] = 10, ["transitions"] = Event("S70", "S160")
        }),
        Step("S70", "EVALUATE", new()
        {
            ["condition_type"] = "DATA_MATCH", ["left"] = "${book_action}", ["operator"] = "EQUAL", ["right"] = "RATE",
            ["transitions"] = Branch("S160", "S80")
        }),
        Step("S80", "DISPLAY", new()
        {
            ["ui_screen"] = "MAP_LOCATION", ["data"] = "${book_detail.location}",
            ["transitions"] = Success("S90")
        }),
        Step("S90", "WAIT", new()
        {
            ["wait_for"] = "USER_TOUCH", ["event_type"] = "MAP_ACTION",
            ["allowed_values"] = new[] { "START_GUIDANCE", "RATE" }, ["result_variable"] = "map_action",
            ["timeout_sec"] = 10, ["transitions"] = Event("S100", "S160")
        }),
        Step("S100", "EVALUATE", new()
        {
            ["condition_type"] = "DATA_MATCH", ["left"] = "${map_action}", ["operator"] = "EQUAL", ["right"] = "RATE",
            ["transitions"] = Branch("S160", "S110")
        }),
        Step("S110", "DISPLAY", new()
        {
            ["ui_screen"] = "NAV_GUIDE",
            ["data"] = new Dictionary<string, object?> { ["destination"] = "${book_detail.location}", ["show_route"] = true },
            ["transitions"] = Success("S120")
        }),
        Step("S120", "MOVE", new()
        {
            ["target"] = "${book_detail.location.endpoint}", ["speed_mode"] = "SLOW_GUIDE",
            ["obstacle_policy"] = "WAIT_RESUME", ["timeout_sec"] = 180,
            ["output_mapping"] = new Dictionary<string, object?> { ["move_status"] = "move_status", ["final_pose"] = "current_pose" },
            ["transitions"] = new Dictionary<string, object?> { ["on_success"] = "S130", ["on_error"] = "S160", ["on_timeout"] = "S160" }
        }),
        Step("S130", "DISPLAY", new()
        {
            ["ui_screen"] = "ARRIVED", ["data"] = "${book_detail.location}",
            ["transitions"] = Success("S140")
        }),
        Step("S140", "NOTIFY", new()
        {
            ["channel"] = "SPEAKER_TTS", ["message_type"] = "INFO",
            ["content"] = "Đã đến vị trí sách. Vui lòng xem kệ và tầng kệ hiển thị trên màn hình.",
            ["transitions"] = Success("S150")
        }),
        Step("S150", "WAIT", new()
        {
            ["wait_for"] = "TIMEOUT", ["timeout_sec"] = 15,
            ["transitions"] = new Dictionary<string, object?> { ["on_timeout"] = "S160", ["on_success"] = "S160" }
        }),
        Step("S160", "DISPLAY", new()
        {
            ["ui_screen"] = "RATING", ["data"] = new Dictionary<string, object?> { ["prompt"] = "Đánh giá trải nghiệm của bạn" },
            ["transitions"] = Success("S170")
        }),
        Step("S170", "WAIT", new()
        {
            ["wait_for"] = "USER_TOUCH", ["event_type"] = "RATING_SUBMIT",
            ["result_variable"] = "rating", ["timeout_sec"] = 10,
            ["transitions"] = Event("S180", "S190")
        }),
        Step("S180", "PROCESS_DATA", new()
        {
            ["action"] = "SAVE_RATING",
            ["payload"] = new Dictionary<string, object?> { ["rating"] = "${rating}", ["task_id"] = "${task_id}" },
            ["transitions"] = new Dictionary<string, object?> { ["on_success"] = "S190", ["on_error"] = "S190" }
        }),
        Step("S190", "DISPLAY", new()
        {
            ["ui_screen"] = "HOME", ["data"] = new Dictionary<string, object?>()
        })
    ];

    private static IReadOnlyDictionary<string, object?> Step(string id, string type, Dictionary<string, object?> values)
    {
        values["id"] = id;
        values["name"] = id;
        values["type"] = type;
        return values;
    }

    private static Dictionary<string, object?> Success(string next) => new() { ["on_success"] = next, ["on_error"] = "S190" };
    private static Dictionary<string, object?> Event(string next, string timeout) => new() { ["on_event"] = next, ["on_timeout"] = timeout, ["on_cancel"] = timeout };
    private static Dictionary<string, object?> Branch(string whenTrue, string whenFalse) => new() { ["on_true"] = whenTrue, ["on_false"] = whenFalse };
}
