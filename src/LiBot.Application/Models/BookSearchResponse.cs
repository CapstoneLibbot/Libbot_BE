using System.Text.Json;
using System.Text.Json.Serialization;

namespace LiBot.Application.Models;

public sealed record BookSearchResponse(
    IReadOnlyList<BookSearchItem> Books,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
