namespace LiBot.Application.Models.Aisle;

public class AisleResponse(
    Guid Id,
    Guid FloorId,
    string Name,
    string? Description,
    string Status,
    AisleFloorResponse? Floor = null,
    IReadOnlyList<AisleShelfResponse>? Shelves = null)
{
    public Guid Id { get; } = Id;
    public Guid FloorId { get; } = FloorId;
    public string Name { get; } = Name;
    public string? Description { get; } = Description;
    public string Status { get; } = Status;
    public AisleFloorResponse? Floor { get; } = Floor;
    public IReadOnlyList<AisleShelfResponse>? Shelves { get; } = Shelves;
}
