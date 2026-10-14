using System.Text.Json;

namespace LiBot.Contracts.Workflow;

public sealed record FacilitySearchResponse(IReadOnlyList<FacilitySearchItem> Facilities);

public sealed record FacilitySearchItem(
    Guid FacilityId,
    string Name,
    string Type,
    Guid MapId,
    string MapName);

public sealed record FacilityGuidanceEndpoint(
    Guid EndpointId,
    string Name,
    double X,
    double Y,
    double Theta);

public sealed record FacilityLocation(
    Guid MapId,
    string MapName,
    int MapWidth,
    int MapHeight,
    FacilityGuidanceEndpoint Endpoint);

public sealed record FacilityGuidanceDetail(
    Guid FacilityId,
    string Name,
    string Type,
    string? Description,
    FacilityLocation Location,
    IReadOnlyList<string> Actions);

public sealed record FacilityFeedbackRequest(Guid TaskId, int Rating, string? Comment = null);

public sealed record FacilityFeedbackResponse(Guid FeedbackId, Guid TaskId, int Rating);

public sealed record StartFacilityGuidanceRequest(string Query, string? BackendBaseUrl = null);

public sealed record FacilityGuidanceEventRequest(string EventType, JsonElement? Data = null);

public sealed record CompleteFacilityGuidanceRequest(string Status, string? Message = null);
