using System.Text.Json;
using System.Text.Json.Serialization;

namespace LiBot.Application.Models;

public sealed record BookGuidanceBookItem(
    Guid Id,
    string Barcode,
    string Status,
    BookLocation? Location);
