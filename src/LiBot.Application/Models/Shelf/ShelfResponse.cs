using LiBot.Application.Models.ShelfLevel;

namespace LiBot.Application.Models.Shelf;

public class ShelfResponse(Guid Id, Guid AisleId, Guid StartEndpointId, Guid EndEndpointId,
    string Name, string? Description, string Status, ShelfAisleResponse? Aisle = null,
    IReadOnlyList<ShelfLevelResponse>? Levels = null)
{
    public Guid Id { get; } = Id;
    public Guid AisleId { get; } = AisleId;
    public Guid StartEndpointId { get; } = StartEndpointId;
    public Guid EndEndpointId { get; } = EndEndpointId;
    public string Name { get; } = Name;
    public string? Description { get; } = Description;
    public string Status { get; } = Status;
    public ShelfAisleResponse? Aisle { get; } = Aisle;
    public IReadOnlyList<ShelfLevelResponse>? Levels { get; } = Levels;
}
