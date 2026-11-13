namespace LiBot.Application.Models.Floor;

public class FloorResponse
{
    public FloorResponse(Guid id, string name, int floorNumber, string? description, string status,
        IReadOnlyList<FloorAisleResponse>? aisles = null)
        : this(id, name, floorNumber, description, status, default, null, aisles) { }

    public FloorResponse(Guid id, string name, int floorNumber, string? description, string status,
        DateTime createdAtUtc, DateTime? updatedAtUtc, IReadOnlyList<FloorAisleResponse>? aisles = null)
    {
        Id = id; Name = name; FloorNumber = floorNumber; Description = description; Status = status;
        CreatedAtUtc = createdAtUtc; UpdatedAtUtc = updatedAtUtc; Aisles = aisles;
    }

    public Guid Id { get; }
    public string Name { get; }
    public int FloorNumber { get; }
    public string? Description { get; }
    public string Status { get; }
    public DateTime CreatedAtUtc { get; }
    public DateTime? UpdatedAtUtc { get; }
    public IReadOnlyList<FloorAisleResponse>? Aisles { get; }
}
