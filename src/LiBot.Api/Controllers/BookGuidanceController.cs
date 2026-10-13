using System.Text.Json;
using LiBot.Application.Robot;
using LiBot.Contracts.Workflow;
using LiBot.Domain.Mission;
using LiBot.Infrastructure.Persistence;
using LiBot.Infrastructure.Robot;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LiBot.Api.Controllers;

[ApiController]
[Route("api/book-guidance")]
public sealed class BookGuidanceController(LiBotDbContext db, ILiBotTcpClient tcpClient) : ControllerBase
{
    [HttpGet("search")]
    public async Task<ActionResult<BookSearchResponse>> Search(
        [FromQuery] string query,
        [FromQuery] string matchMode = "FUZZY",
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return BadRequest("A search query is required.");
        if (!string.Equals(matchMode, "FUZZY", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(matchMode, "EXACT", StringComparison.OrdinalIgnoreCase))
            return BadRequest("matchMode must be FUZZY or EXACT.");

        var term = query.Trim();
        var exact = string.Equals(matchMode, "EXACT", StringComparison.OrdinalIgnoreCase);
        var books = await (
            from book in db.Books.AsNoTracking()
            join category in db.BookCategories.AsNoTracking() on book.CategoryId equals category.Id into categories
            from category in categories.DefaultIfEmpty()
            where exact
                ? book.Title == term || book.Isbn == term || book.Author == term || (category != null && category.Name == term)
                : EF.Functions.Like(book.Title, $"%{term}%") || EF.Functions.Like(book.Isbn, $"%{term}%") ||
                  EF.Functions.Like(book.Author, $"%{term}%") || (category != null && EF.Functions.Like(category.Name, $"%{term}%"))
            orderby book.Title
            select new BookSearchItem(
                book.Id, book.Isbn, book.Title, book.Author, category == null ? null : category.Name,
                db.BookItems.Count(item => item.BookId == book.Id && item.Status == "Available")))
            .Take(50)
            .ToListAsync(cancellationToken);

        return Ok(new BookSearchResponse(books));
    }

    [HttpGet("books/{bookId:guid}")]
    public async Task<ActionResult<BookGuidanceDetail>> GetBook(Guid bookId, CancellationToken cancellationToken)
    {
        var book = await (
            from item in db.Books.AsNoTracking()
            join category in db.BookCategories.AsNoTracking() on item.CategoryId equals category.Id into categories
            from category in categories.DefaultIfEmpty()
            where item.Id == bookId
            select new { item.Id, item.Isbn, item.Title, item.Author, item.Publisher, item.PublicationYear, Classification = category == null ? null : category.Name })
            .FirstOrDefaultAsync(cancellationToken);
        if (book is null) return NotFound("Book not found.");

        var location = await (
            from copy in db.BookItems.AsNoTracking()
            join level in db.ShelfLevels.AsNoTracking() on copy.ExpectedShelfLevelId equals level.Id
            join shelf in db.Shelves.AsNoTracking() on level.ShelfId equals shelf.Id
            join aisle in db.Aisles.AsNoTracking() on shelf.AisleId equals aisle.Id
            join floor in db.Floors.AsNoTracking() on aisle.FloorId equals floor.Id
            join endpoint in db.Endpoints.AsNoTracking() on aisle.EndpointId equals endpoint.Id into endpoints
            from endpoint in endpoints.DefaultIfEmpty()
            where copy.BookId == bookId && copy.Status == "Available"
            orderby copy.Barcode
            select new BookLocation(
                floor.Id, floor.Name, floor.FloorNumber, aisle.Id, aisle.Code, aisle.Name,
                shelf.Id, shelf.Code, shelf.Name, level.Id, level.LevelNumber,
                endpoint == null ? null : new NavigationEndpoint(endpoint.Id, endpoint.Name, endpoint.PositionX, endpoint.PositionY, 0)))
            .FirstOrDefaultAsync(cancellationToken);

        return Ok(new BookGuidanceDetail(
            book.Id, book.Isbn, book.Title, book.Author, book.Publisher, book.PublicationYear,
            book.Classification, location, ["VIEW_LOCATION", "RATE"]));
    }

    [HttpPost("feedback")]
    public async Task<ActionResult<FeedbackResponse>> SaveFeedback(FeedbackRequest request, CancellationToken cancellationToken)
    {
        if (request.Rating is < 1 or > 5) return BadRequest("Rating must be between 1 and 5.");
        if (!await db.Jobs.AnyAsync(job => job.Id == request.TaskId, cancellationToken))
            return NotFound("Task was not found.");

        var feedback = new Feedback(request.TaskId, request.Rating, request.Comment);
        db.Feedback.Add(feedback);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new FeedbackResponse(feedback.Id, feedback.JobId, feedback.Rating));
    }

    [HttpGet("workflow")]
    public async Task<IActionResult> GetWorkflow(CancellationToken cancellationToken)
    {
        LoadedWorkflow? loadedWorkflow;
        try
        {
            loadedWorkflow = await LoadWorkflow(cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            return WorkflowConfigurationProblem(exception.Message);
        }

        if (loadedWorkflow is null)
        {
            return WorkflowConfigurationProblem(
                $"No active workflow with type '{BookGuidanceWorkflow.Type}' was found.");
        }

        return Ok(new
        {
            workflowId = loadedWorkflow.Id,
            type = loadedWorkflow.Type,
            name = loadedWorkflow.Name,
            steps = loadedWorkflow.Steps
        });
    }

    [HttpPost("start")]
    public async Task<IActionResult> Start(StartBookGuidanceRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Query)) return BadRequest("A search query is required.");

        LoadedWorkflow? loadedWorkflow;
        try
        {
            loadedWorkflow = await LoadWorkflow(cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            return WorkflowConfigurationProblem(exception.Message);
        }

        if (loadedWorkflow is null)
        {
            return WorkflowConfigurationProblem(
                $"No active workflow with type '{BookGuidanceWorkflow.Type}' was found.");
        }

        var mission = new Mission("Search & Guide to Book", $"Reader search: {request.Query.Trim()}");
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

        var baseUrl = string.IsNullOrWhiteSpace(request.BackendBaseUrl)
            ? $"{Request.Scheme}://{Request.Host}{Request.PathBase}"
            : request.BackendBaseUrl.TrimEnd('/');
        var tcpRequest = new LiBotStartWorkflowRequest(
            "start_workflow",
            mission.Id.ToString(),
            mission.Id.ToString(),
            new Dictionary<string, object?>
            {
                ["query"] = request.Query.Trim(),
                ["task_id"] = mission.Id.ToString(),
                ["backend_base_url"] = baseUrl
            },
            loadedWorkflow.Steps);

        try
        {
            using var response = await tcpClient.SendAsync(tcpRequest, cancellationToken);
            var body = response.RootElement.Clone();
            if (!IsAccepted(body))
            {
                mission.Fail();
                await db.SaveChangesAsync(cancellationToken);
                return UnprocessableEntity(new { missionId = mission.Id, robot = body });
            }
            return Accepted(new
            {
                missionId = mission.Id,
                executionId = mission.Id,
                workflowId = loadedWorkflow.Id,
                robot = body
            });
        }
        catch (LiBotTcpException exception)
        {
            mission.Fail();
            await db.SaveChangesAsync(cancellationToken);
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "LiBot is unavailable", detail: exception.Message);
        }
    }

    [HttpPost("events")]
    public async Task<IActionResult> SubmitEvent(BookGuidanceEventRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.EventType)) return BadRequest("eventType is required.");
        try
        {
            using var response = await tcpClient.SendAsync(new
            {
                command = "submit_event",
                event_type = request.EventType.Trim().ToUpperInvariant(),
                data = request.Data
            }, cancellationToken);
            return Ok(response.RootElement.Clone());
        }
        catch (LiBotTcpException exception)
        {
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "LiBot is unavailable", detail: exception.Message);
        }
    }

    [HttpGet("executions/{executionId}")]
    public async Task<IActionResult> GetExecution(string executionId, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await tcpClient.SendAsync(new { command = "get_execution", execution_id = executionId }, cancellationToken);
            return Ok(response.RootElement.Clone());
        }
        catch (LiBotTcpException exception)
        {
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "LiBot is unavailable", detail: exception.Message);
        }
    }

    [HttpPost("tasks/{taskId:guid}/complete")]
    public async Task<IActionResult> CompleteTask(Guid taskId, CompleteBookGuidanceRequest request, CancellationToken cancellationToken)
    {
        var mission = await db.Jobs.FirstOrDefaultAsync(job => job.Id == taskId, cancellationToken);
        if (mission is null) return NotFound();
        if (mission.Status is "Completed" or "Failed" or "Cancelled") return NoContent();

        if (string.Equals(request.Status, "SUCCEEDED", StringComparison.OrdinalIgnoreCase)) mission.Complete();
        else mission.Fail();
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static bool IsAccepted(JsonElement response) =>
        response.ValueKind == JsonValueKind.Object && response.TryGetProperty("status", out var status) &&
        status.ValueKind == JsonValueKind.String && string.Equals(status.GetString(), "accepted", StringComparison.OrdinalIgnoreCase);

    private async Task<LoadedWorkflow?> LoadWorkflow(CancellationToken cancellationToken)
    {
        var workflows = await db.Workflows
            .AsNoTracking()
            .Where(item => item.Type == BookGuidanceWorkflow.Type && item.IsActive)
            .OrderBy(item => item.Id)
            .Take(2)
            .ToListAsync(cancellationToken);

        if (workflows.Count > 1)
        {
            throw new InvalidOperationException(
                $"More than one active workflow has type '{BookGuidanceWorkflow.Type}'.");
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
            throw new InvalidOperationException($"Workflow '{workflow.Id}' does not contain any tasks.");
        }

        if (tasks.Select(item => item.SequenceNumber).Distinct().Count() != tasks.Count)
        {
            throw new InvalidOperationException($"Workflow '{workflow.Id}' contains duplicate task sequence numbers.");
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
                throw new InvalidOperationException(
                    $"Workflow task '{task.Name}' ({task.Id}) does not contain a step.");
            }

            if (configuredSteps.Count > 1)
            {
                throw new InvalidOperationException(
                    $"Workflow task '{task.Name}' ({task.Id}) contains more than one step.");
            }

            var taskStep = configuredSteps[0];
            var definition = ParseStepConfiguration(taskStep.Configuration, task.Name, taskStep.StepType);
            steps.Add(definition);
        }

        return new LoadedWorkflow(workflow.Id, workflow.Type, workflow.Name, steps);
    }

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
                throw new InvalidOperationException(
                    $"Step '{taskName}' contains invalid JSON configuration.",
                    exception);
            }

            using (document)
            {
                if (document.RootElement.ValueKind != JsonValueKind.Object)
                {
                    throw new InvalidOperationException(
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

    private ObjectResult WorkflowConfigurationProblem(string detail) => Problem(
        statusCode: StatusCodes.Status500InternalServerError,
        title: "Book guidance workflow is not configured correctly",
        detail: detail);

    private sealed record LoadedWorkflow(
        Guid Id,
        string Type,
        string Name,
        IReadOnlyList<IReadOnlyDictionary<string, object?>> Steps);
}

public sealed record BookSearchResponse(IReadOnlyList<BookSearchItem> Books);
public sealed record BookSearchItem(Guid BookId, string Isbn, string Title, string Author, string? Classification, int AvailableCopies);
public sealed record NavigationEndpoint(Guid EndpointId, string Name, double X, double Y, double Theta);
public sealed record BookLocation(Guid FloorId, string Floor, int FloorNumber, Guid AisleId, string Aisle, string AisleName,
    Guid ShelfId, string Shelf, string ShelfName, Guid ShelfLevelId, int ShelfLevel, NavigationEndpoint? Endpoint);
public sealed record BookGuidanceDetail(Guid BookId, string Isbn, string Title, string Author, string? Publisher,
    int? PublicationYear, string? Classification, BookLocation? Location, IReadOnlyList<string> Actions);
public sealed record FeedbackRequest(Guid TaskId, int Rating, string? Comment = null);
public sealed record FeedbackResponse(Guid FeedbackId, Guid TaskId, int Rating);
public sealed record StartBookGuidanceRequest(string Query, string? BackendBaseUrl = null);
public sealed record BookGuidanceEventRequest(string EventType, JsonElement? Data = null);
public sealed record CompleteBookGuidanceRequest(string Status, string? Message = null);
