using System.Text.Json;

namespace LiBot.Application.Models;

public sealed record FacilitySearchResponse(
    IReadOnlyList<FacilitySearchItem> Facilities,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
