using System.Text.Json;
using System.Text.Json.Nodes;

namespace LiBot.Api.Middleware;

public static class ApiErrorFormatter
{
    public static int GetStatusCode(Exception exception) => exception switch
    {
        ArgumentException => StatusCodes.Status400BadRequest,
        KeyNotFoundException => StatusCodes.Status404NotFound,
        UnauthorizedAccessException => StatusCodes.Status403Forbidden,
        TimeoutException => StatusCodes.Status504GatewayTimeout,
        HttpRequestException => StatusCodes.Status502BadGateway,
        InvalidOperationException => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status500InternalServerError
    };

    public static string Localize(int statusCode, string? detail, string? code = null)
    {
        if (!string.IsNullOrWhiteSpace(detail) && ContainsVietnamese(detail))
            return detail;

        var text = detail?.Trim() ?? string.Empty;
        var normalized = text.ToLowerInvariant();
        var resource = ResourceName(normalized);

        if (statusCode == StatusCodes.Status401Unauthorized)
            return "Bạn chưa được xác thực hoặc thông tin xác thực không hợp lệ.";
        if (statusCode == StatusCodes.Status403Forbidden)
            return "Bạn không có quyền thực hiện thao tác này.";
        if (statusCode == StatusCodes.Status404NotFound)
            return resource is null
                ? "Không tìm thấy tài nguyên được yêu cầu."
                : $"Không tìm thấy {resource}.";
        if (statusCode == StatusCodes.Status502BadGateway)
            return "Không thể nhận phản hồi hợp lệ từ dịch vụ robot.";
        if (statusCode == StatusCodes.Status503ServiceUnavailable)
            return "Dịch vụ robot hiện không khả dụng.";
        if (statusCode == StatusCodes.Status504GatewayTimeout)
            return "Yêu cầu đến dịch vụ robot đã hết thời gian chờ.";

        if (normalized.Contains("required") || normalized.Contains("must be provided") || normalized.Contains("is missing"))
            return "Dữ liệu yêu cầu còn thiếu thông tin bắt buộc.";
        if (normalized.Contains("invalid") || normalized.Contains("unsupported") || normalized.Contains("not valid"))
            return "Dữ liệu yêu cầu không hợp lệ.";
        if (normalized.Contains("already") || normalized.Contains("conflict") || normalized.Contains("cannot be"))
            return "Yêu cầu xung đột với trạng thái hiện tại của dữ liệu.";
        if (normalized.Contains("timed out") || normalized.Contains("timeout"))
            return "Yêu cầu đã hết thời gian chờ.";
        if (normalized.Contains("not configured") || normalized.Contains("configuration"))
            return "Hệ thống chưa được cấu hình đầy đủ cho thao tác này.";

        return statusCode switch
        {
            StatusCodes.Status400BadRequest => "Dữ liệu yêu cầu không hợp lệ.",
            StatusCodes.Status409Conflict => "Yêu cầu xung đột với trạng thái hiện tại của dữ liệu.",
            StatusCodes.Status422UnprocessableEntity => "Dữ liệu không thể xử lý theo quy tắc hiện tại.",
            >= 500 => "Hệ thống gặp lỗi khi xử lý yêu cầu. Vui lòng thử lại sau.",
            _ => "Yêu cầu không thể được xử lý."
        };
    }

    public static JsonObject Normalize(int statusCode, object? value, string path)
    {
        var node = ToJsonObject(value);
        var code = ReadString(node, "code") ?? ReadString(node, "errorCode");
        var detail = ReadString(node, "message")
            ?? ReadString(node, "error")
            ?? ReadString(node, "detail")
            ?? ReadString(node, "title");
        var message = Localize(statusCode, detail, code);

        if (node.ContainsKey("message")) node["message"] = message;
        else if (node.ContainsKey("error")) node["error"] = message;
        else if (node.ContainsKey("detail")) node["detail"] = message;
        else node["message"] = message;

        if (node.ContainsKey("title")) node["title"] = StatusTitle(statusCode);
        node["success"] ??= false;
        node["path"] ??= path;
        node["statusCode"] ??= statusCode;
        if (!string.IsNullOrWhiteSpace(code)) node["code"] ??= code;
        return node;
    }

    private static JsonObject ToJsonObject(object? value)
    {
        if (value is JsonObject jsonObject)
            return jsonObject;
        if (value is string text)
            return new JsonObject { ["message"] = text };
        if (value is JsonElement element)
        {
            var elementNode = JsonNode.Parse(element.GetRawText());
            return elementNode is JsonObject objectNode
                ? objectNode
                : new JsonObject { ["data"] = elementNode };
        }

        try
        {
            var node = JsonSerializer.SerializeToNode(value);
            return node is JsonObject objectNode
                ? objectNode
                : new JsonObject { ["data"] = node };
        }
        catch (JsonException)
        {
            return new JsonObject();
        }
    }

    private static string? ReadString(JsonObject value, string name) =>
        value[name]?.GetValueKind() == JsonValueKind.String ? value[name]!.GetValue<string>() : null;

    private static string StatusTitle(int statusCode) => statusCode switch
    {
        400 => "Yêu cầu không hợp lệ",
        401 => "Chưa xác thực",
        403 => "Không có quyền",
        404 => "Không tìm thấy tài nguyên",
        409 => "Xung đột dữ liệu",
        422 => "Dữ liệu không thể xử lý",
        >= 500 => "Lỗi hệ thống",
        _ => "Lỗi yêu cầu"
    };

    private static string? ResourceName(string text)
    {
        if (text.Contains("book item") || text.Contains("book copy")) return "bản sách";
        if (text.Contains("book") || text.Contains("isbn")) return "sách";
        if (text.Contains("facility")) return "khu vực thư viện";
        if (text.Contains("endpoint")) return "điểm điều hướng";
        if (text.Contains("robot")) return "robot";
        if (text.Contains("workflow")) return "workflow";
        if (text.Contains("job") || text.Contains("task")) return "nhiệm vụ";
        if (text.Contains("map")) return "bản đồ";
        if (text.Contains("shelf")) return "kệ sách";
        return null;
    }

    private static bool ContainsVietnamese(string value) => value.Any(character =>
        character is >= '\u00C0' and <= '\u024F' or >= '\u1E00' and <= '\u1EFF');
}
