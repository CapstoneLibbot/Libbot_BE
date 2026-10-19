using System.Text.Json;
using LiBot.Api.Controllers;
using LiBot.Domain.Workflow;
using LiBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using LibraryEndpoint = LiBot.Domain.Library.Endpoint;

namespace LiBot.Api.Services;

public sealed class WorkflowDocumentService(LiBotDbContext db)
{
    private static readonly HashSet<string> SupportedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "MOVE", "SCAN", "PROCESS_DATA", "EVALUATE", "DISPLAY", "WAIT", "NOTIFY", "END"
    };

    public IReadOnlyList<WorkflowTemplateResponse> GetTemplates() => WorkflowTemplates.All;

    public WorkflowTemplateResponse? GetTemplate(string templateId) =>
        WorkflowTemplates.All.FirstOrDefault(item =>
            item.Id.Equals(templateId, StringComparison.OrdinalIgnoreCase));

    public string? ValidateSteps(IReadOnlyList<WorkflowStepWriteRequest> steps)
    {
        if (steps.Count == 0) return "A workflow must contain at least one step.";
        if (steps.Any(item => !SupportedTypes.Contains(item.Type ?? string.Empty)))
            return "Workflow contains an unsupported step type. Supported types: " + string.Join(", ", SupportedTypes.Order());
        if (steps.Any(item => item.Sequence < 1)) return "Step sequence numbers must be positive.";
        if (steps.Select(item => item.Sequence).Distinct().Count() != steps.Count)
            return "Step sequence numbers must be unique.";

        var ordered = steps.OrderBy(item => item.Sequence).ToList();
        var ids = ordered.Select((item, index) => GetStepId(item.Configuration, item.Name, index)).ToList();
        if (ids.Distinct(StringComparer.Ordinal).Count() != ids.Count)
            return "Workflow step ids must be unique.";

        for (var index = 0; index < ordered.Count; index++)
        {
            var step = ordered[index];
            var config = step.Configuration;
            if (config is null || config is { ValueKind: not JsonValueKind.Object })
                return $"Step '{ids[index]}' configuration must be a JSON object.";
            var objectConfiguration = config.Value;
            if (string.Equals(step.Type, "MOVE", StringComparison.OrdinalIgnoreCase))
            {
                var hasTarget = objectConfiguration.TryGetProperty("target", out var target);
                var hasEndpointId = objectConfiguration.TryGetProperty("endpoint_id", out var endpointId);
                var hasEndpointName = objectConfiguration.TryGetProperty("endpoint_name", out var endpointName);
                var hasX = objectConfiguration.TryGetProperty("x", out var x);
                var hasY = objectConfiguration.TryGetProperty("y", out var y);
                if (!hasTarget && !hasEndpointId && !hasEndpointName && !(hasX && hasY))
                    return $"MOVE step '{ids[index]}' needs target, endpoint_id, endpoint_name, or x/y coordinates.";
                if (hasEndpointId && (endpointId.ValueKind != JsonValueKind.String || !Guid.TryParse(endpointId.GetString(), out _)))
                    return $"MOVE step '{ids[index]}' endpoint_id must be a valid GUID.";
                if (hasEndpointName && (endpointName.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(endpointName.GetString())))
                    return $"MOVE step '{ids[index]}' endpoint_name cannot be empty.";
                if (hasX != hasY || (hasX && (!IsFiniteNumber(x) || !IsFiniteNumber(y))))
                    return $"MOVE step '{ids[index]}' coordinates must include finite numeric x and y values.";
                if (hasTarget && target.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
                    return $"MOVE step '{ids[index]}' target cannot be empty.";
                if (hasTarget && target.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(target.GetString()))
                    return $"MOVE step '{ids[index]}' target cannot be empty.";
                if (hasTarget && target.ValueKind == JsonValueKind.Object
                    && !(target.TryGetProperty("endpoint_id", out _) || target.TryGetProperty("id", out _)
                         || (target.TryGetProperty("x", out var targetX) && target.TryGetProperty("y", out var targetY)
                             && IsFiniteNumber(targetX) && IsFiniteNumber(targetY))))
                    return $"MOVE step '{ids[index]}' target must contain endpoint_id, id, or finite x/y coordinates.";
            }

            if (config is { ValueKind: JsonValueKind.Object } configured
                && string.Equals(step.Type, "WAIT", StringComparison.OrdinalIgnoreCase))
            {
                var waitFor = ReadString(configured, "wait_for") ?? "DURATION";
                if (waitFor is not ("USER_TOUCH" or "CONTEXT" or "DURATION" or "TIMEOUT"))
                    return $"WAIT step '{ids[index]}' has unsupported wait_for '{waitFor}'.";
                if (waitFor == "USER_TOUCH" && string.IsNullOrWhiteSpace(ReadString(configured, "event_type")))
                    return $"WAIT step '{ids[index]}' with USER_TOUCH requires event_type.";
                if (configured.TryGetProperty("allowed_values", out var allowedValues)
                    && allowedValues.ValueKind != JsonValueKind.Array)
                    return $"WAIT step '{ids[index]}' allowed_values must be an array.";
            }

            if (config is { ValueKind: JsonValueKind.Object } processConfig
                && string.Equals(step.Type, "PROCESS_DATA", StringComparison.OrdinalIgnoreCase))
            {
                var action = ReadString(processConfig, "action") ?? ReadString(processConfig, "operation");
                var supportedActions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "SEARCH_BOOK", "GET_BOOK_DETAIL", "SAVE_RATING", "SEARCH_FACILITY", "GET_FACILITY_DETAIL",
                    "SAVE_FACILITY_RATING", "RECORD_INVENTORY", "FINALIZE_INVENTORY",
                    "RECORD_PATROL_OBSERVATION", "FINALIZE_PATROL", "IDENTITY"
                };
                if (string.IsNullOrWhiteSpace(action) || !supportedActions.Contains(action))
                    return $"PROCESS_DATA step '{ids[index]}' has unsupported action '{action ?? "<missing>"}'.";
            }

            if (string.Equals(step.Type, "EVALUATE", StringComparison.OrdinalIgnoreCase))
            {
                var operation = ReadString(objectConfiguration, "operator") ?? "EQUAL";
                if (operation.ToUpperInvariant() is not ("EQUAL" or "NOT_EQUAL" or "IN"))
                    return $"EVALUATE step '{ids[index]}' has unsupported operator '{operation}'.";
            }

            if (string.Equals(step.Type, "NOTIFY", StringComparison.OrdinalIgnoreCase))
            {
                var channel = ReadString(objectConfiguration, "channel") ?? "LOG";
                if (channel.ToUpperInvariant() is not ("LOG" or "SPEAKER_TTS"))
                    return $"NOTIFY step '{ids[index]}' has unsupported channel '{channel}'.";
            }

            if (string.Equals(step.Type, "END", StringComparison.OrdinalIgnoreCase))
            {
                var status = ReadString(objectConfiguration, "result_status") ?? ReadString(objectConfiguration, "status") ?? "SUCCEEDED";
                if (status.ToUpperInvariant() is not ("SUCCEEDED" or "FAILED" or "CANCELLED"))
                    return $"END step '{ids[index]}' has unsupported status '{status}'.";
            }

            var transitionError = ValidateTransitions(objectConfiguration, ids, ids[index]);
            if (transitionError is not null) return transitionError;
        }

        return null;
    }

    public async Task<RobotWorkflowDocument?> BuildRobotDocumentAsync(
        Guid workflowId,
        string runtimeId,
        CancellationToken cancellationToken)
    {
        var workflow = await db.Workflows.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == workflowId, cancellationToken);
        if (workflow is null) return null;

        var tasks = await db.WorkflowTasks.AsNoTracking()
            .Where(item => item.WorkflowId == workflowId)
            .OrderBy(item => item.SequenceNumber)
            .ToListAsync(cancellationToken);
        var taskIds = tasks.Select(item => item.Id).ToList();
        var taskSteps = await db.TaskSteps.AsNoTracking()
            .Where(item => taskIds.Contains(item.WorkflowTaskId))
            .ToListAsync(cancellationToken);
        var stepsByTask = taskSteps.GroupBy(item => item.WorkflowTaskId)
            .ToDictionary(group => group.Key, group => group.ToList());

        var exportedSteps = new List<JsonElement>(tasks.Count);
        for (var index = 0; index < tasks.Count; index++)
        {
            var task = tasks[index];
            if (!stepsByTask.TryGetValue(task.Id, out var matching) || matching.Count != 1)
                throw new InvalidWorkflowDefinitionException(
                    $"Task '{task.Name}' must contain exactly one configured step.");

            var taskStep = matching[0];
            if (!SupportedTypes.Contains(taskStep.StepType))
                throw new InvalidWorkflowDefinitionException(
                    $"Step '{task.Name}' uses unsupported type '{taskStep.StepType}'.");

            var config = ParseConfiguration(taskStep.Configuration, task.Name);
            var stepId = GetStepId(config, task.Name, index);
            var definition = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in config.EnumerateObject()) definition[property.Name] = property.Value.Clone();
            definition["id"] = JsonSerializer.SerializeToElement(stepId);
            definition["name"] = JsonSerializer.SerializeToElement(task.Name);
            definition["type"] = JsonSerializer.SerializeToElement(taskStep.StepType.ToUpperInvariant());

            var endpointId = ReadGuid(config, "endpoint_id");
            var endpointName = ReadString(config, "endpoint_name");
            var targetText = ReadString(config, "target");
            if (endpointId is null && endpointName is null && !string.IsNullOrWhiteSpace(targetText)
                && !targetText.Contains("${", StringComparison.Ordinal))
            {
                if (Guid.TryParse(targetText, out var targetId)) endpointId = targetId;
                else endpointName = targetText;
            }
            if (endpointId is not null || endpointName is not null)
            {
                var endpointQuery = db.Endpoints.AsNoTracking().Where(item => item.IsActive);
                LibraryEndpoint? endpoint = endpointId is Guid id
                    ? await endpointQuery.FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
                    : await endpointQuery.FirstOrDefaultAsync(item => item.Name == endpointName, cancellationToken);
                if (endpoint is null)
                    throw new InvalidWorkflowDefinitionException(
                        $"MOVE step '{task.Name}' refers to an inactive or unknown endpoint.");
                definition.Remove("endpoint_id");
                definition.Remove("endpoint_name");
                definition["target"] = JsonSerializer.SerializeToElement(new
                {
                    x = endpoint.PositionX,
                    y = endpoint.PositionY,
                    theta = endpoint.Theta
                });
            }
            else if (config.TryGetProperty("x", out var x) && config.TryGetProperty("y", out var y))
            {
                definition["target"] = JsonSerializer.SerializeToElement(new
                {
                    x = x.GetDouble(),
                    y = y.GetDouble(),
                    theta = config.TryGetProperty("theta", out var theta) ? theta.GetDouble() : 0d
                });
                definition.Remove("x");
                definition.Remove("y");
                definition.Remove("theta");
            }

            exportedSteps.Add(JsonSerializer.SerializeToElement(definition));
        }

        var writeSteps = tasks.Select((task, index) => new WorkflowStepWriteRequest(
            task.SequenceNumber,
            stepsByTask[task.Id][0].StepType,
            task.Name,
            ParseConfiguration(stepsByTask[task.Id][0].Configuration, task.Name))).ToList();
        var validationError = ValidateSteps(writeSteps);
        if (validationError is not null) throw new InvalidWorkflowDefinitionException(validationError);

        return new RobotWorkflowDocument(
            1,
            workflow.Id,
            workflow.Name,
            workflow.Type,
            runtimeId.ToLowerInvariant(),
            exportedSteps);
    }

    private static JsonElement ParseConfiguration(string? configuration, string stepName)
    {
        if (string.IsNullOrWhiteSpace(configuration)) return JsonSerializer.SerializeToElement(new { });
        try
        {
            using var document = JsonDocument.Parse(configuration);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                throw new InvalidWorkflowDefinitionException($"Step '{stepName}' configuration must be a JSON object.");
            return document.RootElement.Clone();
        }
        catch (JsonException exception)
        {
            throw new InvalidWorkflowDefinitionException($"Step '{stepName}' contains invalid JSON configuration.", exception);
        }
    }

    private static string GetStepId(JsonElement? configuration, string? name, int index)
    {
        if (configuration is { ValueKind: JsonValueKind.Object } config
            && config.TryGetProperty("id", out var configuredId)
            && configuredId.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(configuredId.GetString()))
            return configuredId.GetString()!;
        return string.IsNullOrWhiteSpace(name) ? $"step-{index + 1}" : name;
    }

    private static IEnumerable<string> TransitionTargets(JsonElement config)
    {
        var transitionProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "on_success", "on_error", "on_timeout", "on_event", "on_cancel",
            "on_true", "on_false", "on_true_step", "on_false_step", "on_timeout_step", "on_cancel_step"
        };
        foreach (var property in config.EnumerateObject())
        {
            if (property.Name.Equals("transitions", StringComparison.OrdinalIgnoreCase)
                && property.Value.ValueKind == JsonValueKind.Object)
            {
                foreach (var transition in property.Value.EnumerateObject())
                    if (transitionProperties.Contains(transition.Name) && transition.Value.ValueKind == JsonValueKind.String)
                        yield return transition.Value.GetString()!;
            }
            else if (transitionProperties.Contains(property.Name) && property.Value.ValueKind == JsonValueKind.String)
            {
                yield return property.Value.GetString()!;
            }
        }
    }

    private static string? ValidateTransitions(JsonElement config, IReadOnlyList<string> ids, string stepId)
    {
        var transitionProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "on_success", "on_error", "on_timeout", "on_event", "on_cancel",
            "on_true", "on_false", "on_true_step", "on_false_step", "on_timeout_step", "on_cancel_step"
        };
        foreach (var property in config.EnumerateObject())
        {
            if (property.Name.Equals("transitions", StringComparison.OrdinalIgnoreCase))
            {
                if (property.Value.ValueKind != JsonValueKind.Object)
                    return $"Step '{stepId}' transitions must be an object.";
                foreach (var transition in property.Value.EnumerateObject())
                {
                    if (!transitionProperties.Contains(transition.Name))
                        return $"Step '{stepId}' has unsupported transition '{transition.Name}'.";
                    if (transition.Name != transition.Name.ToLowerInvariant())
                        return $"Step '{stepId}' transition names must use lowercase snake_case.";
                    if (transition.Value.ValueKind != JsonValueKind.String)
                        return $"Step '{stepId}' transition '{transition.Name}' must name a step id.";
                    var target = transition.Value.GetString()!;
                    if (!ids.Contains(target, StringComparer.Ordinal))
                        return $"Step '{stepId}' has a transition to unknown step '{target}'.";
                }
            }
            else if (transitionProperties.Contains(property.Name))
            {
                if (property.Name != property.Name.ToLowerInvariant())
                    return $"Step '{stepId}' transition names must use lowercase snake_case.";
                if (property.Value.ValueKind != JsonValueKind.String)
                    return $"Step '{stepId}' transition '{property.Name}' must name a step id.";
                var target = property.Value.GetString()!;
                if (!ids.Contains(target, StringComparer.Ordinal))
                    return $"Step '{stepId}' has a transition to unknown step '{target}'.";
            }
            else if (property.Name.StartsWith("on_", StringComparison.OrdinalIgnoreCase))
            {
                return $"Step '{stepId}' has unsupported transition '{property.Name}'.";
            }
        }
        return null;
    }

    private static bool IsFiniteNumber(JsonElement value) =>
        value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var number) && double.IsFinite(number);

    private static Guid? ReadGuid(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            && Guid.TryParse(value.GetString(), out var parsed) ? parsed : null;

    private static string? ReadString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() : null;
}

public sealed record RobotWorkflowDocument(
    int Version,
    Guid WorkflowId,
    string Name,
    string Type,
    string RuntimeId,
    IReadOnlyList<JsonElement> Steps);

public sealed record WorkflowTemplateResponse(
    string Id,
    string Name,
    string Description,
    string Type,
    IReadOnlyList<WorkflowTemplateStep> Steps);

public sealed record WorkflowTemplateStep(
    int Sequence,
    string Type,
    string Name,
    JsonElement Configuration);

public sealed class InvalidWorkflowDefinitionException(string message, Exception? inner = null) : Exception(message, inner);

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
            MakeStep(1, "S10", "PROCESS_DATA", new() { ["action"] = "SEARCH_BOOK", ["payload"] = new { query = "${query}", runtime_id = "${runtime_id}" }, ["output_mapping"] = new { data = "search_results" }, ["transitions"] = new { on_success = "S20", on_error = "S90" } }),
            MakeStep(2, "S20", "DISPLAY", new() { ["ui_screen"] = "BOOK_SEARCH_RESULTS", ["data"] = "${search_results}", ["transitions"] = new { on_success = "S30", on_error = "S90" } }),
            MakeStep(3, "S30", "WAIT", new() { ["wait_for"] = "USER_TOUCH", ["event_type"] = "BOOK_SELECTED", ["result_variable"] = "selected_book_id", ["timeout_sec"] = 120, ["transitions"] = new { on_event = "S40", on_timeout = "S90", on_cancel = "S90" } }),
            MakeStep(4, "S40", "PROCESS_DATA", new() { ["action"] = "GET_BOOK_DETAIL", ["payload"] = new { book_id = "${selected_book_id}", runtime_id = "${runtime_id}" }, ["output_mapping"] = new { data = "book_detail" }, ["transitions"] = new { on_success = "S50", on_error = "S90" } }),
            MakeStep(5, "S50", "DISPLAY", new() { ["ui_screen"] = "BOOK_INFO", ["data"] = "${book_detail}", ["transitions"] = new { on_success = "S60", on_error = "S90" } }),
            MakeStep(6, "S60", "MOVE", new() { ["target"] = "${book_detail.location.endpoint}", ["timeout_sec"] = 180, ["transitions"] = new { on_success = "S70", on_error = "S90", on_timeout = "S90" } }),
            MakeStep(7, "S70", "DISPLAY", new() { ["ui_screen"] = "ARRIVED", ["data"] = "${book_detail.location}", ["transitions"] = new { on_success = "S80", on_error = "S90" } }),
            MakeStep(8, "S80", "END", new() { ["status"] = "SUCCEEDED" }),
            MakeStep(9, "S90", "END", new() { ["status"] = "FAILED" })
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
            MakeStep(6, "P50", "PROCESS_DATA", new() { ["action"] = "FINALIZE_PATROL", ["payload"] = new { mission_id = "${mission_id}", runtime_id = "${runtime_id}" }, ["transitions"] = new { on_success = "P60", on_error = "P70" } }),
            MakeStep(7, "P60", "NOTIFY", new() { ["channel"] = "SPEAKER_TTS", ["content"] = "Library patrol completed.", ["transitions"] = new { on_success = "P80", on_error = "P70" } }),
            MakeStep(8, "P70", "END", new() { ["status"] = "FAILED" }),
            MakeStep(9, "P80", "END", new() { ["status"] = "SUCCEEDED" })
        }),
        new("misplaced-book-detection", "Phát hiện sách sai vị trí", "Quét barcode tại kệ và ghi nhận các bản sao sai kệ.", "MISPLACED_BOOK_DETECTION", new[]
        {
            MakeStep(1, "M10", "MOVE", new() { ["endpoint_name"] = "Kệ A3", ["transitions"] = new { on_success = "M20", on_error = "M80" } }),
            MakeStep(2, "M20", "SCAN", new() { ["sensor"] = "camera", ["device"] = "imx500", ["output_key"] = "scanned_code", ["timeout_sec"] = 15, ["transitions"] = new { on_success = "M30", on_timeout = "M40", on_error = "M80" } }),
            MakeStep(3, "M30", "PROCESS_DATA", new() { ["action"] = "RECORD_INVENTORY", ["payload"] = new { barcode = "${scanned_code}", shelf_code = "A3", mission_id = "${mission_id}", runtime_id = "${runtime_id}" }, ["transitions"] = new { on_success = "M40", on_error = "M80" } }),
            MakeStep(4, "M40", "WAIT", new() { ["wait_for"] = "USER_TOUCH", ["event_type"] = "INVENTORY_ACTION", ["allowed_values"] = new[] { "CONTINUE", "DONE" }, ["result_variable"] = "inventory_action", ["timeout_sec"] = 300, ["transitions"] = new { on_event = "M45", on_timeout = "M80" } }),
            MakeStep(5, "M45", "EVALUATE", new() { ["left"] = "${inventory_action}", ["operator"] = "EQUAL", ["right"] = "DONE", ["transitions"] = new { on_true = "M50", on_false = "M20" } }),
            MakeStep(6, "M50", "PROCESS_DATA", new() { ["action"] = "FINALIZE_INVENTORY", ["payload"] = new { mission_id = "${mission_id}", runtime_id = "${runtime_id}", shelf_code = "A3", report_type = "MISPLACED_BOOK_DETECTION" }, ["transitions"] = new { on_success = "M55", on_error = "M80" } }),
            MakeStep(7, "M55", "NOTIFY", new() { ["channel"] = "SPEAKER_TTS", ["content"] = "Misplaced book scan completed.", ["transitions"] = new { on_success = "M70", on_error = "M80" } }),
            MakeStep(8, "M70", "END", new() { ["status"] = "SUCCEEDED" }),
            MakeStep(9, "M80", "END", new() { ["status"] = "FAILED" })
        })
    ];
}
