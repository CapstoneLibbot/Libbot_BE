using LiBot.Domain.Enums;

namespace LiBot.Application.Models.Aisle;

public class AisleResponse(
    Guid Id,
    Guid FloorId,
    string Name,
    string? Description,
    AisleStatus Status,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    AisleFloorResponse? Floor = null,
    IReadOnlyList<AisleShelfResponse>? Shelves = null)
{
    public Guid Id { get; } = Id;
    public Guid FloorId { get; } = FloorId;
    public string Name { get; } = Name;
    public string? Description { get; } = Description;
    public AisleStatus Status { get; } = Status;
    public DateTime CreatedAtUtc { get; } = CreatedAtUtc;
    public DateTime? UpdatedAtUtc { get; } = UpdatedAtUtc;

    public AisleFloorResponse? Floor { get; } = Floor;
    public IReadOnlyList<AisleShelfResponse>? Shelves { get; } = Shelves;
}