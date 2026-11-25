using System.Text.Json;
using LiBot.Application.Robot;
using LiBot.Application.Interfaces;
using LiBot.Application.Workflow.BookGuidance;
using LiBot.Application.Models;
using LiBot.Application.Services.Workflow;
using LiBot.Application.Workflow;
using LiBot.Contracts.Workflow;
using LiBot.Domain.Enums;
using LiBot.Domain.Job;
using LiBot.Infrastructure.Robot;

namespace LiBot.Infrastructure.Workflow;

public sealed class BookGuidanceService(
    IBookGuidanceRepository books,
    IGuidanceWorkflowRepository workflowRepository,
    ILiBotTcpClient tcpClient,
    BookGuidanceRuntimeRegistry runtimeRegistry,
    IRobotTaskRepository robotTasks,
    IWorkflowDocumentService workflowDocuments,
    IJobDataService jobData) : IBookGuidanceService
{
    public Task<bool> IsSimulationTaskAsync(Guid taskId, CancellationToken cancellationToken) =>
        robotTasks.IsSimulationTaskAsync(taskId, cancellationToken);

    public async Task<BookSearchResponse> SearchAsync(
        string? query,
        string? category,
        string matchMode,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var exact = string.Equals(matchMode, "EXACT", StringComparison.OrdinalIgnoreCase);
        return await books.SearchAsync(query, category, exact, page, pageSize, cancellationToken);
    }

    public Task<BookSearchResponse> SearchTitlesAsync(
        string query,
        int page,
        int pageSize,
        CancellationToken cancellationToken) =>
        books.SearchTitlesAsync(query, page, pageSize, cancellationToken);

    public Task<BookGuidanceDetail?> GetBookAsync(
        Guid bookId,
        CancellationToken cancellationToken)
        => books.GetBookAsync(bookId, cancellationToken);

    public Task<BookCopyStartTarget?> GetCopyStartTargetAsync(
        Guid bookId,
        Guid bookItemId,
        CancellationToken cancellationToken)
        => books.GetCopyStartTargetAsync(bookId, bookItemId, cancellationToken);

    public Task<FeedbackResponse?> SaveFeedbackAsync(
        FeedbackRequest request,
        CancellationToken cancellationToken)
        => books.SaveFeedbackAsync(request, cancellationToken);

    public Task<IReadOnlyList<FeedbackResponse>> GetFeedbackAsync(Guid? taskId, CancellationToken cancellationToken) =>
        books.GetFeedbackAsync(taskId, cancellationToken);

    public Task<FeedbackResponse?> GetFeedbackByIdAsync(Guid feedbackId, CancellationToken cancellationToken) =>
        books.GetFeedbackByIdAsync(feedbackId, cancellationToken);

    public Task<FeedbackResponse?> UpdateFeedbackAsync(Guid feedbackId, FeedbackUpdateRequest request, CancellationToken cancellationToken) =>
        books.UpdateFeedbackAsync(feedbackId, request, cancellationToken);

    public Task<bool> DeleteFeedbackAsync(Guid feedbackId, CancellationToken cancellationToken) =>
        books.DeleteFeedbackAsync(feedbackId, cancellationToken);

    public async Task<BookGuidanceLoadedWorkflow?> LoadWorkflowAsync(CancellationToken cancellationToken)
    {
        var activeWorkflows = await workflowRepository.GetActiveWorkflowsAsync(BookGuidanceWorkflow.Type, cancellationToken);

        if (activeWorkflows.Count > 1)
        {
            throw new BookGuidanceWorkflowConfigurationException(
                $"More than one active workflow has type '{BookGuidanceWorkflow.Type}'.");
        }

        var workflow = activeWorkflows.SingleOrDefault();
        if (workflow is null) return null;

        if (workflow.Tasks.Count == 0)
        {
            throw new BookGuidanceWorkflowConfigurationException(
                $"Workflow '{workflow.Id}' does not contain any tasks.");
        }

        if (workflow.Tasks.Select(item => item.SequenceNumber).Distinct().Count() != workflow.Tasks.Count)
        {
            throw new BookGuidanceWorkflowConfigurationException(
                $"Workflow '{workflow.Id}' contains duplicate task sequence numbers.");
        }

        var steps = new List<IReadOnlyDictionary<string, object?>>(workflow.Tasks.Count);
        foreach (var task in workflow.Tasks)
        {
            if (task.Steps.Count == 0)
            {
                throw new BookGuidanceWorkflowConfigurationException(
                    $"Workflow task '{task.Name}' ({task.Id}) does not contain a step.");
            }

            foreach (var taskStep in task.Steps.OrderBy(item => item.SequenceNumber))
            {
                var stepName = task.Steps.Count == 1 ? task.Name : $"{task.Name} {taskStep.SequenceNumber}";
                var definition = ParseStepConfiguration(taskStep.Configuration, stepName, taskStep.StepType);
                steps.Add(definition);
            }
        }

        var primaryTask = workflow.Tasks.OrderBy(item => item.SequenceNumber).First();
        return new BookGuidanceLoadedWorkflow(workflow.Id, primaryTask.Id, primaryTask.SequenceNumber,
            primaryTask.Name, workflow.Type, workflow.Name, steps);
    }

    public async Task<BookGuidanceStartResult> StartAsync(
        StartBookGuidanceRequest request,
        string backendBaseUrl,
        CancellationToken cancellationToken)
    {
        var loadedWorkflow = await LoadWorkflowAsync(cancellationToken);
        if (loadedWorkflow is null)
        {
            return new BookGuidanceStartResult(Guid.Empty, null, default, false);
        }

        var queryText = request.Query?.Trim() ?? string.Empty;
        var job = new Job("Search & Guide to Book", $"Reader search: {(string.IsNullOrEmpty(queryText) ? "Guidance" : queryText)}",
            request.RobotId, loadedWorkflow.Id);
        job.Start();
        var runtimeId = string.IsNullOrWhiteSpace(request.RuntimeId)
            ? "sim"
            : request.RuntimeId.Trim().ToLowerInvariant();
        RobotWorkflowDocument workflowDocument;
        try
        {
            workflowDocument = await workflowDocuments.BuildRobotDocumentAsync(loadedWorkflow.Id, runtimeId, cancellationToken)
                ?? throw new BookGuidanceWorkflowConfigurationException("The active book guidance workflow was not found.");
        }
        catch (InvalidWorkflowDefinitionException exception)
        {
            throw new BookGuidanceWorkflowConfigurationException(exception.Message, exception);
        }
        var jobTask = new JobTask(job.Id, loadedWorkflow.PrimaryTaskId, loadedWorkflow.PrimaryTaskSequence,
            loadedWorkflow.PrimaryTaskName, runtimeId);
        jobTask.Start();
        jobTask.SetExecutionId(job.Id.ToString());
        jobTask.UpdateExecutionState(JsonSerializer.Serialize(new
        {
            jobId = job.Id,
            executionId = job.Id,
            runtimeId,
            status = "ACCEPTED",
            updatedAt = DateTime.UtcNow
        }));
        var contextValues = new Dictionary<string, object?>
        {
            ["query"] = queryText,
            ["book_id"] = request.BookId?.ToString(),
            ["book_item_id"] = request.BookItemId?.ToString(),
            ["endpoint_id"] = request.EndpointId?.ToString(),
            ["runtime_id"] = runtimeId,
            ["task_id"] = job.Id.ToString(),
            ["backend_base_url"] = backendBaseUrl
        };
        var initialContext = JsonSerializer.Serialize(contextValues);
        await workflowRepository.CreateExecutionAsync(job, jobTask, workflowDocument, initialContext, cancellationToken);
        var persistedWorkflow = await jobData.GetRobotWorkflowDocumentAsync(job.Id, cancellationToken)
            ?? throw new BookGuidanceWorkflowConfigurationException("The job step configuration could not be loaded.");

        var tcpRequest = new LiBotStartWorkflowRequest(
            "start_workflow",
            job.Id.ToString(),
            job.Id.ToString(),
            contextValues,
            persistedWorkflow.Steps.Select(ToStepDictionary).ToArray());

        try
        {
            using var response = await tcpClient.SendAsync(runtimeId, tcpRequest, cancellationToken);
            var body = response.RootElement.Clone();
            var isAccepted = IsAccepted(body);
            if (!isAccepted)
            {
                job.Fail();
                jobTask.UpdateExecutionState(JsonSerializer.Serialize(new
                {
                    jobId = job.Id,
                    executionId = job.Id,
                    runtimeId,
                    status = "FAILED",
                    error = ReadMessage(body),
                    updatedAt = DateTime.UtcNow
                }));
                await workflowRepository.SaveChangesAsync(cancellationToken);
            }
            else
            {
                runtimeRegistry.Register(job.Id.ToString(), runtimeId);
            }

            return new BookGuidanceStartResult(job.Id, loadedWorkflow, body, isAccepted);
        }
        catch (LiBotTcpException exception)
        {
            job.Fail();
            jobTask.UpdateExecutionState(JsonSerializer.Serialize(new
            {
                jobId = job.Id,
                executionId = job.Id,
                runtimeId,
                status = "FAILED",
                error = exception.Message,
                updatedAt = DateTime.UtcNow
            }));
            await workflowRepository.SaveChangesAsync(cancellationToken);
            throw new BookGuidanceRobotUnavailableException(exception.Message, exception);
        }
    }

    public async Task<JsonElement> SubmitEventAsync(
        BookGuidanceEventRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var executionId = request.ExecutionId?.Trim();
            if (string.IsNullOrWhiteSpace(executionId)
                || !runtimeRegistry.TryGetRuntime(executionId, out var runtimeId))
            {
                throw new BookGuidanceExecutionNotFoundException(executionId ?? "");
            }

            using var response = await tcpClient.SendAsync(runtimeId, new
            {
                command = "submit_event",
                execution_id = executionId,
                event_type = request.EventType.Trim().ToUpperInvariant(),
                data = request.Data
            }, cancellationToken);
            return response.RootElement.Clone();
        }
        catch (LiBotTcpException exception)
        {
            throw new BookGuidanceRobotUnavailableException(exception.Message, exception);
        }
    }

    public async Task<JsonElement> GetExecutionAsync(
        string executionId,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!runtimeRegistry.TryGetRuntime(executionId, out var runtimeId))
                throw new BookGuidanceExecutionNotFoundException(executionId);

            using var response = await tcpClient.SendAsync(
                runtimeId,
                new { command = "get_execution", execution_id = executionId },
                cancellationToken);
            return response.RootElement.Clone();
        }
        catch (LiBotTcpException exception)
        {
            throw new BookGuidanceRobotUnavailableException(exception.Message, exception);
        }
    }

    public async Task<bool> CompleteTaskAsync(
        Guid taskId,
        CompleteBookGuidanceRequest request,
        CancellationToken cancellationToken)
    {
        var job = await workflowRepository.GetJobAsync(taskId, cancellationToken);
        if (job is null) return false;
        if (job.StatusCode is JobStatus.Completed or JobStatus.Failed or JobStatus.Cancelled) return true;

        if (string.Equals(request.Status, "SUCCEEDED", StringComparison.OrdinalIgnoreCase)) job.Complete();
        else job.Fail();
        await workflowRepository.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static bool IsAccepted(JsonElement response) =>
        response.ValueKind == JsonValueKind.Object &&
        response.TryGetProperty("status", out var status) &&
        status.ValueKind == JsonValueKind.String &&
        string.Equals(status.GetString(), "accepted", StringComparison.OrdinalIgnoreCase);

    private static string? ReadMessage(JsonElement response) =>
        response.ValueKind == JsonValueKind.Object &&
        response.TryGetProperty("message", out var message) &&
        message.ValueKind == JsonValueKind.String
            ? message.GetString()
            : null;

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
                throw new BookGuidanceWorkflowConfigurationException(
                    $"Step '{taskName}' contains invalid JSON configuration.",
                    exception);
            }

            using (document)
            {
                if (document.RootElement.ValueKind != JsonValueKind.Object)
                {
                    throw new BookGuidanceWorkflowConfigurationException(
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

    private static IReadOnlyDictionary<string, object?> ToStepDictionary(JsonElement step) =>
        step.EnumerateObject().ToDictionary(property => property.Name,
            property => (object?)property.Value.Clone(), StringComparer.OrdinalIgnoreCase);
}
