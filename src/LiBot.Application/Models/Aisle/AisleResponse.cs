namespace LiBot.Application.Models.Aisle;

public class AisleResponse
{
    public AisleResponse(Guid id, Guid floorId, string name, string? description, string status,
        AisleFloorResponse? floor = null, IReadOnlyList<AisleShelfResponse>? shelves = null)
        : this(id, floorId, name, description, status, default, null, floor, shelves) { }

    public AisleResponse(Guid id, Guid floorId, string name, string? description, string status,
        DateTime createdAtUtc, DateTime? updatedAtUtc, AisleFloorResponse? floor = null,
        IReadOnlyList<AisleShelfResponse>? shelves = null)
    {
        Id = id;
        FloorId = floorId;
        Name = name;
        Description = description;
        Status = status;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        Floor = floor;
        Shelves = shelves;
    }

    public Guid Id { get; }
    public Guid FloorId { get; }
    public string Name { get; }
    public string? Description { get; }
    public string Status { get; }
    public DateTime CreatedAtUtc { get; }
    public DateTime? UpdatedAtUtc { get; }
    public AisleFloorResponse? Floor { get; }
    public IReadOnlyList<AisleShelfResponse>? Shelves { get; }
}
