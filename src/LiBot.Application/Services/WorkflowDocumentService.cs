using System.Text.Json;
using LiBot.Application.Models;
using LiBot.Application.Services.Workflow;
using LiBot.Application.Workflow;
using LiBot.Domain.Workflow;

namespace LiBot.Application.Services;

public sealed class WorkflowDocumentService(IWorkflowDocumentRepository repository) : IWorkflowDocumentService
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
            var stepConfiguration = objectConfiguration;
            if (objectConfiguration.TryGetProperty("params", out var parameters))
            {
                if (parameters.ValueKind != JsonValueKind.Object)
                    return $"Step '{ids[index]}' params must be a JSON object.";
                if (!objectConfiguration.TryGetProperty("id", out var envelopeId)
                    || envelopeId.ValueKind != JsonValueKind.String
                    || string.IsNullOrWhiteSpace(envelopeId.GetString()))
                    return $"Step '{ids[index]}' envelope id is required.";
                if (!objectConfiguration.TryGetProperty("type", out var envelopeType)
                    || envelopeType.ValueKind != JsonValueKind.String
                    || !string.Equals(envelopeType.GetString(), step.Type, StringComparison.OrdinalIgnoreCase))
                    return $"Step '{ids[index]}' envelope type must match the configured step type.";
                if (!objectConfiguration.TryGetProperty("name", out var envelopeName)
                    || envelopeName.ValueKind != JsonValueKind.String
                    || string.IsNullOrWhiteSpace(envelopeName.GetString()))
                    return $"Step '{ids[index]}' envelope name is required.";
                if (!objectConfiguration.TryGetProperty("output_mapping", out var outputMapping)
                    || outputMapping.ValueKind != JsonValueKind.Object)
                    return $"Step '{ids[index]}' output_mapping must be an object.";
                if (!objectConfiguration.TryGetProperty("transitions", out var envelopeTransitions)
                    || envelopeTransitions.ValueKind != JsonValueKind.Object)
                    return $"Step '{ids[index]}' transitions must be an object.";
                if (!objectConfiguration.TryGetProperty("policy", out var policy)
                    || policy.ValueKind != JsonValueKind.Object)
                    return $"Step '{ids[index]}' policy must be an object.";
                stepConfiguration = parameters;
            }
            if (string.Equals(step.Type, "MOVE", StringComparison.OrdinalIgnoreCase))
            {
                var hasTarget = stepConfiguration.TryGetProperty("target", out var target);
                var hasEndpointId = stepConfiguration.TryGetProperty("endpoint_id", out var endpointId);
                var hasEndpointName = stepConfiguration.TryGetProperty("endpoint_name", out var endpointName);
                var hasX = stepConfiguration.TryGetProperty("x", out var x);
                var hasY = stepConfiguration.TryGetProperty("y", out var y);
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

            if (string.Equals(step.Type, "WAIT", StringComparison.OrdinalIgnoreCase))
            {
                var waitFor = ReadString(stepConfiguration, "wait_for") ?? "DURATION";
                if (waitFor is not ("USER_TOUCH" or "CONTEXT" or "DURATION" or "TIMEOUT"))
                    return $"WAIT step '{ids[index]}' has unsupported wait_for '{waitFor}'.";
                if (waitFor == "USER_TOUCH" && string.IsNullOrWhiteSpace(ReadString(stepConfiguration, "event_type")))
                    return $"WAIT step '{ids[index]}' with USER_TOUCH requires event_type.";
                if (stepConfiguration.TryGetProperty("allowed_values", out var allowedValues)
                    && allowedValues.ValueKind != JsonValueKind.Array)
                    return $"WAIT step '{ids[index]}' allowed_values must be an array.";
            }

            if (string.Equals(step.Type, "PROCESS_DATA", StringComparison.OrdinalIgnoreCase))
            {
                var action = ReadString(stepConfiguration, "action") ?? ReadString(stepConfiguration, "operation");
                var supportedActions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "SEARCH_BOOK", "GET_BOOK_DETAIL", "SAVE_RATING", "SEARCH_FACILITY", "GET_FACILITY_DETAIL",
                    "SAVE_FACILITY_RATING", "RECORD_INVENTORY", "ADD_INVENTORY_ISSUE", "FINALIZE_INVENTORY",
                    "RECORD_PATROL_OBSERVATION", "GENERATE_PATROL_REPORT", "FINALIZE_PATROL", "IDENTITY",
                    "PATROL_SCOPE", "BUILD_PATROL_COVERAGE_PLAN", "CLASSIFY_PATROL_OBSERVATIONS", "UPDATE_PATH_COVERAGE"
                };
                if (string.IsNullOrWhiteSpace(action) || !supportedActions.Contains(action))
                    return $"PROCESS_DATA step '{ids[index]}' has unsupported action '{action ?? "<missing>"}'.";
            }

            if (string.Equals(step.Type, "EVALUATE", StringComparison.OrdinalIgnoreCase))
            {
                var operation = ReadString(stepConfiguration, "operator") ?? "EQUAL";
                if (operation.ToUpperInvariant() is not ("EQUAL" or "NOT_EQUAL" or "IN" or "LESS_THAN_OR_EQUAL" or "GREATER_THAN"))
                    return $"EVALUATE step '{ids[index]}' has unsupported operator '{operation}'.";
            }

            if (string.Equals(step.Type, "NOTIFY", StringComparison.OrdinalIgnoreCase))
            {
                var channel = ReadString(stepConfiguration, "channel") ?? "LOG";
                if (channel.ToUpperInvariant() is not ("LOG" or "SPEAKER_TTS" or "LIBRARIAN_WS"))
                    return $"NOTIFY step '{ids[index]}' has unsupported channel '{channel}'.";
            }

            if (string.Equals(step.Type, "END", StringComparison.OrdinalIgnoreCase))
            {
                var status = ReadString(stepConfiguration, "result_status") ?? ReadString(stepConfiguration, "status") ?? "SUCCEEDED";
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
        var source = await repository.GetDocumentSourceAsync(workflowId, cancellationToken);
        if (source is null) return null;
        var workflow = source.Workflow;
        var tasks = source.Tasks;
        var taskSteps = source.Steps;
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

            var nestedParameters = config.TryGetProperty("params", out var configuredParams);
            var movementConfig = nestedParameters ? configuredParams : config;
            var endpointId = ReadGuid(movementConfig, "endpoint_id");
            var endpointName = ReadString(movementConfig, "endpoint_name");
            var targetText = ReadString(movementConfig, "target");
            if (endpointId is null && endpointName is null && !string.IsNullOrWhiteSpace(targetText)
                && !targetText.Contains("${", StringComparison.Ordinal))
            {
                if (Guid.TryParse(targetText, out var targetId)) endpointId = targetId;
                else endpointName = targetText;
            }
            if (endpointId is not null || endpointName is not null)
            {
                var endpoint = await repository.GetActiveEndpointAsync(endpointId, endpointName, cancellationToken);
                if (endpoint is null)
                    throw new InvalidWorkflowDefinitionException(
                        $"MOVE step '{task.Name}' refers to an inactive or unknown endpoint.");
                var updatedParameters = movementConfig.EnumerateObject()
                    .Where(property => !property.Name.Equals("endpoint_id", StringComparison.OrdinalIgnoreCase)
                        && !property.Name.Equals("endpoint_name", StringComparison.OrdinalIgnoreCase))
                    .ToDictionary(property => property.Name, property => property.Value.Clone(), StringComparer.OrdinalIgnoreCase);
                updatedParameters["target"] = JsonSerializer.SerializeToElement(new
                {
                    x = endpoint.PositionX,
                    y = endpoint.PositionY,
                    theta = endpoint.Theta
                });
                if (nestedParameters) definition["params"] = JsonSerializer.SerializeToElement(updatedParameters);
                else
                {
                    definition.Remove("endpoint_id");
                    definition.Remove("endpoint_name");
                    definition["target"] = updatedParameters["target"];
                }
            }
            else if (movementConfig.TryGetProperty("x", out var x) && movementConfig.TryGetProperty("y", out var y))
            {
                var updatedParameters = movementConfig.EnumerateObject()
                    .Where(property => !property.Name.Equals("x", StringComparison.OrdinalIgnoreCase)
                        && !property.Name.Equals("y", StringComparison.OrdinalIgnoreCase)
                        && !property.Name.Equals("theta", StringComparison.OrdinalIgnoreCase))
                    .ToDictionary(property => property.Name, property => property.Value.Clone(), StringComparer.OrdinalIgnoreCase);
                updatedParameters["target"] = JsonSerializer.SerializeToElement(new
                {
                    x = x.GetDouble(),
                    y = y.GetDouble(),
                    theta = movementConfig.TryGetProperty("theta", out var theta) ? theta.GetDouble() : 0d
                });
                if (nestedParameters) definition["params"] = JsonSerializer.SerializeToElement(updatedParameters);
                else
                {
                    definition["target"] = updatedParameters["target"];
                    definition.Remove("x");
                    definition.Remove("y");
                    definition.Remove("theta");
                }
            }

            exportedSteps.Add(JsonSerializer.SerializeToElement(definition));
        }

        var writeSteps = tasks.Select((task, index) => new WorkflowStepWriteRequest(
            task.SequenceNumber,
            stepsByTask[task.Id][0].StepType,
            task.Name,
            ParseConfiguration(stepsByTask[task.Id][0].Configuration, task.Name),
            Description: task.Description)).ToList();
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

