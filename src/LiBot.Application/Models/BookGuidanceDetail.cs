using System.Text.Json;
using System.Text.Json.Serialization;

namespace LiBot.Application.Models;

public sealed record BookGuidanceDetail(
    Guid BookId,
    string Isbn,
    string Title,
    string Author,
    string? Publisher,
    int? PublicationYear,
    string? Classification,
    string? Image,
    string? Description)
{
    public IReadOnlyList<BookGuidanceBookItem> BookItems { get; init; } = [];
}
