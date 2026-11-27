namespace LiBot.Application.Models;

public static class FacilityGuidanceWorkflow
{
    public const string Type = "SEARCH_AND_GUIDE_TO_FACILITY";

    public static IReadOnlyList<IReadOnlyDictionary<string, object?>> CreateSteps() =>
    [
        Step("S10", "PROCESS_DATA", new()
        {
            ["action"] = "GET_FACILITY_DETAIL",
            ["payload"] = new Dictionary<string, object?>
            {
                ["facility_id"] = "${facility_id}",
                ["runtime_id"] = "${runtime_id}"
            },
            ["output_mapping"] = new Dictionary<string, object?> { ["data"] = "facility_detail" },
            ["transitions"] = Success("S20")
        }),
        Step("S20", "MOVE", new()
        {
            ["target"] = "${facility_detail.location.endpoint}",
            ["speed_mode"] = "SLOW_GUIDE",
            ["obstacle_policy"] = "WAIT_RESUME",
            ["timeout_sec"] = 180,
            ["output_mapping"] = new Dictionary<string, object?>
            {
                ["move_status"] = "move_status",
                ["final_pose"] = "current_pose"
            },
            ["transitions"] = new Dictionary<string, object?>
            {
                ["on_success"] = "S30",
                ["on_error"] = "S60",
                ["on_timeout"] = "S60"
            }
        }),
        Step("S30", "WAIT", new()
        {
            ["wait_for"] = "DURATION",
            ["seconds"] = 2,
            ["timeout_sec"] = 2,
            ["transitions"] = Success("S40")
        }),
        Step("S40", "NOTIFY", new()
        {
            ["channel"] = "SPEAKER_TTS",
            ["message_type"] = "INFO",
            ["content"] = "Đã đến khu vực ${facility_detail.name}. Mời bạn xem thông tin trên màn hình.",
            ["transitions"] = Success("S50")
        }),
        Step("S50", "END", new() { ["result_status"] = "SUCCEEDED" }),
        Step("S60", "END", new() { ["result_status"] = "FAILED" })
    ];

    private static IReadOnlyDictionary<string, object?> Step(string id, string type, Dictionary<string, object?> values)
    {
        values["id"] = id;
        values["name"] = id;
        values["type"] = type;
        return values;
    }

    private static Dictionary<string, object?> Success(string next) => new() { ["on_success"] = next, ["on_error"] = "S60" };
}
