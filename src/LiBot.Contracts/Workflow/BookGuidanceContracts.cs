using System.Text.Json;

namespace LiBot.Contracts.Workflow;

public sealed record BookSearchResponse(IReadOnlyList<BookSearchItem> Books);

public sealed record BookSearchItem(
    Guid BookId,
    string Isbn,
    string Title,
    string Author,
    string? Classification,
    int AvailableCopies);

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

public sealed record BookGuidanceDetail(
    Guid BookId,
    string Isbn,
    string Title,
    string Author,
    string? Publisher,
    int? PublicationYear,
    string? Classification,
    BookLocation? Location,
    IReadOnlyList<string> Actions);

public sealed record FeedbackRequest(Guid TaskId, int Rating, string? Comment = null);

public sealed record FeedbackResponse(Guid FeedbackId, Guid TaskId, int Rating);

public sealed record StartBookGuidanceRequest(string Query, string? BackendBaseUrl = null);

public sealed record BookGuidanceEventRequest(string EventType, JsonElement? Data = null);

public sealed record CompleteBookGuidanceRequest(string Status, string? Message = null);
