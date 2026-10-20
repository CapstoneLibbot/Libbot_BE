using System.Text.Json;
using System.Text.Json.Serialization;

namespace LiBot.Application.Models;

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
