using System.Text.Json;
using LiBot.Application.Robot;
using LiBot.Application.Workflow.BookGuidance;
using LiBot.Contracts.Workflow;
using LiBot.Domain.Mission;
using LiBot.Infrastructure.Persistence;
using LiBot.Infrastructure.Robot;
using Microsoft.EntityFrameworkCore;

namespace LiBot.Infrastructure.Workflow;

public sealed class BookGuidanceService(LiBotDbContext db, ILiBotTcpClient tcpClient) : IBookGuidanceService
{
    public async Task<BookSearchResponse> SearchAsync(
        string? query,
        string? category,
        string matchMode,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var exact = string.Equals(matchMode, "EXACT", StringComparison.OrdinalIgnoreCase);
        var booksQuery =
            from book in db.Books.AsNoTracking()
            join bookCategory in db.BookCategories.AsNoTracking() on book.CategoryId equals bookCategory.Id into categories
            from bookCategory in categories.DefaultIfEmpty()
            select new { Book = book, CategoryName = bookCategory == null ? null : bookCategory.Name };

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            booksQuery = exact
                ? booksQuery.Where(item => item.Book.Title == term || item.Book.Isbn == term || item.Book.Author == term)
                : booksQuery.Where(item =>
                    EF.Functions.Like(item.Book.Title, $"%{term}%") ||
                    EF.Functions.Like(item.Book.Isbn, $"%{term}%") ||
                    EF.Functions.Like(item.Book.Author, $"%{term}%"));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var categoryTerm = category.Trim().ToLowerInvariant();
            booksQuery = booksQuery.Where(item =>
                item.CategoryName != null &&
                EF.Functions.Like(item.CategoryName.ToLower(), $"%{categoryTerm}%"));
        }

        var totalCount = await booksQuery.CountAsync(cancellationToken);
        var books = await booksQuery
            .OrderBy(item => item.Book.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new BookSearchItem(
                item.Book.Id,
                item.Book.Isbn,
                item.Book.Title,
                item.Book.Author,
                item.CategoryName,
                db.BookItems.Count(bookItem => bookItem.BookId == item.Book.Id && bookItem.Status == "Available"),
                item.Book.Image))
            .ToListAsync(cancellationToken);

        return new BookSearchResponse(books, page, pageSize, totalCount, (int)Math.Ceiling(totalCount / (double)pageSize));
    }

    public async Task<BookGuidanceDetail?> GetBookAsync(
        Guid bookId,
        CancellationToken cancellationToken)
    {
        var book = await (
            from item in db.Books.AsNoTracking()
            join category in db.BookCategories.AsNoTracking() on item.CategoryId equals category.Id into categories
            from category in categories.DefaultIfEmpty()
            where item.Id == bookId
            select new
            {
                item.Id,
                item.Isbn,
                item.Title,
                item.Author,
                item.Publisher,
                item.PublicationYear,
                item.Image,
                Classification = category == null ? null : category.Name
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (book is null) return null;

        var copies = await db.BookItems
            .AsNoTracking()
            .Where(copy => copy.BookId == bookId)
            .OrderBy(copy => copy.Barcode)
            .Select(copy => new
            {
                copy.Id,
                copy.Barcode,
                copy.Status
            })
            .ToListAsync(cancellationToken);

        var locationsByCopyId = await (
            from copy in db.BookItems.AsNoTracking()
            join level in db.ShelfLevels.AsNoTracking() on copy.ExpectedShelfLevelId equals level.Id
            join shelf in db.Shelves.AsNoTracking() on level.ShelfId equals shelf.Id
            join aisle in db.Aisles.AsNoTracking() on shelf.AisleId equals aisle.Id
            join floor in db.Floors.AsNoTracking() on aisle.FloorId equals floor.Id
            join endpoint in db.Endpoints.AsNoTracking().Where(item => item.IsActive)
                on aisle.EndpointId equals endpoint.Id into endpoints
            from endpoint in endpoints.DefaultIfEmpty()
            where copy.BookId == bookId
            select new
            {
                copy.Id,
                Location = new BookLocation(
                    floor.Id, floor.Name, floor.FloorNumber, aisle.Id, aisle.Code, aisle.Name,
                    shelf.Id, shelf.Code, shelf.Name, level.Id, level.LevelNumber,
                    endpoint == null ? null : new NavigationEndpoint(
                        endpoint.Id,
                        endpoint.Name,
                        endpoint.PositionX,
                        endpoint.PositionY,
                        endpoint.Theta))
            })
            .ToDictionaryAsync(item => item.Id, item => item.Location, cancellationToken);

        var bookItems = copies
            .Select(copy => new BookGuidanceBookItem(
                copy.Id,
                copy.Barcode,
                copy.Status,
                locationsByCopyId.GetValueOrDefault(copy.Id)))
            .ToList();

        var location = bookItems
            .FirstOrDefault(copy => copy.Status == "Available" && copy.Location is not null)
            ?.Location;

        return new BookGuidanceDetail(
            book.Id, book.Isbn, book.Title, book.Author, book.Publisher, book.PublicationYear,
            book.Classification, location, ["VIEW_LOCATION", "START_GUIDANCE", "RATE"], book.Image)
        {
            BookItems = bookItems
        };
    }

    public async Task<FeedbackResponse?> SaveFeedbackAsync(
        FeedbackRequest request,
        CancellationToken cancellationToken)
    {
        if (!await db.Jobs.AnyAsync(job => job.Id == request.TaskId, cancellationToken))
            return null;

        var feedback = new Feedback(request.TaskId, request.Rating, request.Comment);
        db.Feedback.Add(feedback);
        await db.SaveChangesAsync(cancellationToken);
        return new FeedbackResponse(feedback.Id, feedback.JobId, feedback.Rating);
    }

    public async Task<BookGuidanceLoadedWorkflow?> LoadWorkflowAsync(CancellationToken cancellationToken)
    {
        var workflows = await db.Workflows
            .AsNoTracking()
            .Where(item => item.Type == BookGuidanceWorkflow.Type && item.IsActive)
            .OrderBy(item => item.Id)
            .Take(2)
            .ToListAsync(cancellationToken);

        if (workflows.Count > 1)
        {
            throw new BookGuidanceWorkflowConfigurationException(
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
            throw new BookGuidanceWorkflowConfigurationException(
                $"Workflow '{workflow.Id}' does not contain any tasks.");
        }

        if (tasks.Select(item => item.SequenceNumber).Distinct().Count() != tasks.Count)
        {
            throw new BookGuidanceWorkflowConfigurationException(
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
                throw new BookGuidanceWorkflowConfigurationException(
                    $"Workflow task '{task.Name}' ({task.Id}) does not contain a step.");
            }

            if (configuredSteps.Count > 1)
            {
                throw new BookGuidanceWorkflowConfigurationException(
                    $"Workflow task '{task.Name}' ({task.Id}) contains more than one step.");
            }

            var taskStep = configuredSteps[0];
            var definition = ParseStepConfiguration(taskStep.Configuration, task.Name, taskStep.StepType);
            steps.Add(definition);
        }

        return new BookGuidanceLoadedWorkflow(workflow.Id, workflow.Type, workflow.Name, steps);
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

            return new BookGuidanceStartResult(mission.Id, loadedWorkflow, body, isAccepted);
        }
        catch (LiBotTcpException exception)
        {
            mission.Fail();
            await db.SaveChangesAsync(cancellationToken);
            throw new BookGuidanceRobotUnavailableException(exception.Message, exception);
        }
    }

    public async Task<JsonElement> SubmitEventAsync(
        BookGuidanceEventRequest request,
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
            throw new BookGuidanceRobotUnavailableException(exception.Message, exception);
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
            throw new BookGuidanceRobotUnavailableException(exception.Message, exception);
        }
    }

    public async Task<bool> CompleteTaskAsync(
        Guid taskId,
        CompleteBookGuidanceRequest request,
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
}
