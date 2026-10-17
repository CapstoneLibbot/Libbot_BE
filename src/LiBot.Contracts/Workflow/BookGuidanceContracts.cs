using System.Text.Json;
using System.Text.Json.Serialization;

namespace LiBot.Contracts.Workflow;

public sealed record BookSearchResponse(
    IReadOnlyList<BookSearchItem> Books,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record BookSearchItem(
    Guid BookId,
    string Isbn,
    string Title,
    string Author,
    string? Classification,
    int AvailableCopies,
    string? Image);

public sealed record NavigationEndpoint(
    Guid EndpointId,
    string Name,
    double X,
    double Y,
    double Theta);

public sealed record BookLocation(
    Guid FloorId,
    string Floor,
    int FloorNumber,
    Guid AisleId,
    string Aisle,
    string AisleName,
    Guid ShelfId,
    string Shelf,
    string ShelfName,
    Guid ShelfLevelId,
    int ShelfLevel,
    NavigationEndpoint? Endpoint);

public sealed record BookGuidanceBookItem(
    Guid Id,
    string Barcode,
    string Status,
    BookLocation? Location);

public sealed record BookGuidanceDetail(
    Guid BookId,
    string Isbn,
    string Title,
    string Author,
    string? Publisher,
    int? PublicationYear,
    string? Classification,
    IReadOnlyList<string> Actions,
    string? Image)
{
    public IReadOnlyList<BookGuidanceBookItem> BookItems { get; init; } = [];
}

public sealed record FeedbackRequest(Guid TaskId, int Rating, string? Comment = null);

public sealed record FeedbackResponse(Guid FeedbackId, Guid TaskId, int Rating);

public sealed record StartBookGuidanceRequest(
    string Query,
    string? BackendBaseUrl = null,
    string? RuntimeId = null,
    Guid? BookId = null,
    Guid? BookItemId = null,
    string? ArmLeaseId = null);

public sealed record BookGuidanceEventRequest(
    string EventType,
    JsonElement? Data = null,
    string? ExecutionId = null);

public sealed record CompleteBookGuidanceRequest(string Status, string? Message = null);

public sealed record LiBotStartWorkflowRequest(
    [property: JsonPropertyName("command")] string Command,
    [property: JsonPropertyName("execution_id")] string ExecutionId,
    [property: JsonPropertyName("task_id")] string TaskId,
    [property: JsonPropertyName("initial_context")] IReadOnlyDictionary<string, object?> InitialContext,
    [property: JsonPropertyName("steps")] IReadOnlyList<IReadOnlyDictionary<string, object?>> Steps);
