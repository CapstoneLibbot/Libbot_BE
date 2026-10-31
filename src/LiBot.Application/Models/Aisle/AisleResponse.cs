namespace LiBot.Application.Models.Aisle;

public class AisleResponse(
    Guid Id,
    Guid FloorId,
    Guid? EndpointId,
    string Code,
    string Name,
    string? Description,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    AisleFloorResponse? Floor = null,
    AisleEndpointResponse? Endpoint = null,
    IReadOnlyList<AisleShelfResponse>? Shelves = null)
{
    public Guid Id { get; } = Id;
    public Guid FloorId { get; } = FloorId;
    public Guid? EndpointId { get; } = EndpointId;
    public string Code { get; } = Code;
    public string Name { get; } = Name;
    public string? Description { get; } = Description;
    public DateTime CreatedAtUtc { get; } = CreatedAtUtc;
    public DateTime? UpdatedAtUtc { get; } = UpdatedAtUtc;
    public AisleFloorResponse? Floor { get; } = Floor;
    public AisleEndpointResponse? Endpoint { get; } = Endpoint;
    public IReadOnlyList<AisleShelfResponse>? Shelves { get; } = Shelves;
}
