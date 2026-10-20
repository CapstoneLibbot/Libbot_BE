using System.Text.Json;
using LiBot.Application.Robot;
using LiBot.Application.Workflow.FacilityGuidance;
using LiBot.Application.Models;
using LiBot.Contracts.Workflow;
using LiBot.Domain.Mission;
using LiBot.Infrastructure.Persistence;
using LiBot.Infrastructure.Robot;
using Microsoft.EntityFrameworkCore;

namespace LiBot.Infrastructure.Workflow;

public sealed class FacilityGuidanceService(LiBotDbContext db, ILiBotTcpClient tcpClient) : IFacilityGuidanceService
{
    public async Task<FacilitySearchResponse> SearchAsync(
        string query,
        string matchMode,
        CancellationToken cancellationToken)
    {
        var term = query.Trim();
        var exact = string.Equals(matchMode, "EXACT", StringComparison.OrdinalIgnoreCase);
        var facilitiesQuery =
            from endpoint in db.Endpoints.AsNoTracking()
            join map in db.Maps.AsNoTracking() on endpoint.MapId equals map.Id
            where endpoint.IsActive
            select new { Endpoint = endpoint, Map = map };

        facilitiesQuery = exact
            ? facilitiesQuery.Where(item => item.Endpoint.Name == term || item.Map.Name == term)
            : facilitiesQuery.Where(item =>
                EF.Functions.Like(item.Endpoint.Name, $"%{term}%") ||
                EF.Functions.Like(item.Map.Name, $"%{term}%"));

        var facilities = await facilitiesQuery
            .OrderBy(item => item.Endpoint.Name)
            .Select(item => new FacilitySearchItem(
                item.Endpoint.Id,
                item.Endpoint.Name,
                "ENDPOINT",
                item.Map.Id,
                item.Map.Name))
            .Take(50)
            .ToListAsync(cancellationToken);

        return new FacilitySearchResponse(facilities);
    }

    public async Task<FacilityGuidanceDetail?> GetFacilityAsync(
        Guid facilityId,
        CancellationToken cancellationToken)
    {
        return await (
            from endpoint in db.Endpoints.AsNoTracking()
            join map in db.Maps.AsNoTracking() on endpoint.MapId equals map.Id
            where endpoint.Id == facilityId && endpoint.IsActive
            select new FacilityGuidanceDetail(
                endpoint.Id,
                endpoint.Name,
                "ENDPOINT",
                map.Name,
                new FacilityLocation(
                    map.Id,
                    map.Name,
                    map.Width,
                    map.Height,
                    new FacilityGuidanceEndpoint(
                        endpoint.Id,
                        endpoint.Name,
                        endpoint.PositionX,
                        endpoint.PositionY,
                        endpoint.Theta)),
                new[] { "VIEW_LOCATION", "RATE" }))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<FacilityFeedbackResponse?> SaveFeedbackAsync(
        FacilityFeedbackRequest request,
        CancellationToken cancellationToken)
    {
        if (!await db.Jobs.AnyAsync(job => job.Id == request.TaskId, cancellationToken))
            return null;

        var feedback = new Feedback(request.TaskId, request.Rating, request.Comment);
        db.Feedback.Add(feedback);
        await db.SaveChangesAsync(cancellationToken);
        return new FacilityFeedbackResponse(feedback.Id, feedback.JobId, feedback.Rating);
    }

    public async Task<FacilityGuidanceLoadedWorkflow?> LoadWorkflowAsync(CancellationToken cancellationToken)
    {
        var workflows = await db.Workflows
            .AsNoTracking()
            .Where(item => item.Type == FacilityGuidanceWorkflow.Type && item.IsActive)
            .OrderBy(item => item.Id)
            .Take(2)
            .ToListAsync(cancellationToken);

        if (workflows.Count > 1)
        {
            throw new FacilityGuidanceWorkflowConfigurationException(
                $"More than one active workflow has type '{FacilityGuidanceWorkflow.Type}'.");
        }

        var workflow = workflows.SingleOrDefault();
        if (workflow is null) return null;

        var tasks = await db.WorkflowTasks
            .AsNoTracking()
            .Where(item => item.WorkflowId == workflow.Id)
            .OrderBy(item => item.SequenceNumber)
            .ToListAsync(cancellationToken);

        if (tasks.Count == 0)
        {
            throw new FacilityGuidanceWorkflowConfigurationException(
                $"Workflow '{workflow.Id}' does not contain any tasks.");
        }

        if (tasks.Select(item => item.SequenceNumber).Distinct().Count() != tasks.Count)
        {
            throw new FacilityGuidanceWorkflowConfigurationException(
                $"Workflow '{workflow.Id}' contains duplicate task sequence numbers.");
        }

        var taskIds = tasks.Select(item => item.Id).ToList();
        var taskSteps = await db.TaskSteps
            .AsNoTracking()
            .Where(item => taskIds.Contains(item.WorkflowTaskId))
            .ToListAsync(cancellationToken);
        var stepsByTask = taskSteps
            .GroupBy(item => item.WorkflowTaskId)
            .ToDictionary(group => group.Key, group => group.ToList());

        var steps = new List<IReadOnlyDictionary<string, object?>>(tasks.Count);
        foreach (var task in tasks)
        {
            if (!stepsByTask.TryGetValue(task.Id, out var configuredSteps) || configuredSteps.Count == 0)
            {
                throw new FacilityGuidanceWorkflowConfigurationException(
                    $"Workflow task '{task.Name}' ({task.Id}) does not contain a step.");
            }

            if (configuredSteps.Count > 1)
            {
                throw new FacilityGuidanceWorkflowConfigurationException(
                    $"Workflow task '{task.Name}' ({task.Id}) contains more than one step.");
            }

            var taskStep = configuredSteps[0];
            var definition = ParseStepConfiguration(taskStep.Configuration, task.Name, taskStep.StepType);
            steps.Add(definition);
        }

        return new FacilityGuidanceLoadedWorkflow(workflow.Id, workflow.Type, workflow.Name, steps);
    }

    public async Task<FacilityGuidanceStartResult> StartAsync(
        StartFacilityGuidanceRequest request,
        string backendBaseUrl,
        CancellationToken cancellationToken)
    {
        var loadedWorkflow = await LoadWorkflowAsync(cancellationToken);
        if (loadedWorkflow is null)
        {
            return new FacilityGuidanceStartResult(Guid.Empty, null, default, false);
        }

        var mission = new Mission("Search & Guide to Facility", $"Reader facility search: {request.Query.Trim()}");
        mission.Update(
            mission.Name,
            mission.Description,
            mission.Priority,
            loadedWorkflow.Id,
            mission.SourceEndpointId,
            mission.DestinationEndpointId);
        mission.Start();
        db.Jobs.Add(mission);
        await db.SaveChangesAsync(cancellationToken);

        var tcpRequest = new LiBotStartWorkflowRequest(
            "start_workflow",
            mission.Id.ToString(),
            mission.Id.ToString(),
            new Dictionary<string, object?>
            {
                ["query"] = request.Query.Trim(),
                ["task_id"] = mission.Id.ToString(),
                ["backend_base_url"] = backendBaseUrl
            },
            loadedWorkflow.Steps);

        try
        {
            using var response = await tcpClient.SendAsync(tcpRequest, cancellationToken);
            var body = response.RootElement.Clone();
            var isAccepted = IsAccepted(body);
            if (!isAccepted)
            {
                mission.Fail();
                await db.SaveChangesAsync(cancellationToken);
            }

            return new FacilityGuidanceStartResult(mission.Id, loadedWorkflow, body, isAccepted);
        }
        catch (LiBotTcpException exception)
        {
            mission.Fail();
            await db.SaveChangesAsync(cancellationToken);
            throw new FacilityGuidanceRobotUnavailableException(exception.Message, exception);
        }
    }

    public async Task<JsonElement> SubmitEventAsync(
        FacilityGuidanceEventRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await tcpClient.SendAsync(new
            {
                command = "submit_event",
                event_type = request.EventType.Trim().ToUpperInvariant(),
                data = request.Data
            }, cancellationToken);
            return response.RootElement.Clone();
        }
        catch (LiBotTcpException exception)
        {
            throw new FacilityGuidanceRobotUnavailableException(exception.Message, exception);
        }
    }

    public async Task<JsonElement> GetExecutionAsync(
        string executionId,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await tcpClient.SendAsync(
                new { command = "get_execution", execution_id = executionId },
                cancellationToken);
            return response.RootElement.Clone();
        }
        catch (LiBotTcpException exception)
        {
            throw new FacilityGuidanceRobotUnavailableException(exception.Message, exception);
        }
    }

    public async Task<bool> CompleteTaskAsync(
        Guid taskId,
        CompleteFacilityGuidanceRequest request,
        CancellationToken cancellationToken)
    {
        var mission = await db.Jobs.FirstOrDefaultAsync(job => job.Id == taskId, cancellationToken);
        if (mission is null) return false;
        if (mission.Status is "Completed" or "Failed" or "Cancelled") return true;

        if (string.Equals(request.Status, "SUCCEEDED", StringComparison.OrdinalIgnoreCase)) mission.Complete();
        else mission.Fail();
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static bool IsAccepted(JsonElement response) =>
        response.ValueKind == JsonValueKind.Object &&
        response.TryGetProperty("status", out var status) &&
        status.ValueKind == JsonValueKind.String &&
        string.Equals(status.GetString(), "accepted", StringComparison.OrdinalIgnoreCase);

    private static IReadOnlyDictionary<string, object?> ParseStepConfiguration(
        string? configuration,
        string taskName,
        string stepType)
    {
        var definition = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(configuration))
        {
            JsonDocument document;
            try
            {
                document = JsonDocument.Parse(configuration);
            }
            catch (JsonException exception)
            {
                throw new FacilityGuidanceWorkflowConfigurationException(
                    $"Step '{taskName}' contains invalid JSON configuration.",
                    exception);
            }

            using (document)
            {
                if (document.RootElement.ValueKind != JsonValueKind.Object)
                {
                    throw new FacilityGuidanceWorkflowConfigurationException(
                        $"Step '{taskName}' configuration must be a JSON object.");
                }

                foreach (var property in document.RootElement.EnumerateObject())
                {
                    definition[property.Name] = property.Value.Clone();
                }
            }
        }

        if (!definition.ContainsKey("id")) definition["id"] = taskName;
        if (!definition.ContainsKey("name")) definition["name"] = taskName;
        definition["type"] = stepType;
        return definition;
    }
}
