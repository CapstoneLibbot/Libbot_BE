using System.Text.Json;
using System.Text.Json.Serialization;

namespace LiBot.Application.Models;

public sealed record BookSearchItem(
    Guid BookId,
    string Isbn,
    string Title,
    string Author,
    string? Classification,
    int AvailableCopies,
    string? Image);
