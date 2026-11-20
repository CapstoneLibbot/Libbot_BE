namespace LiBot.Application.Models.Shelf;

public class ShelfWriteRequest(Guid AisleId, Guid StartEndpointId, Guid EndEndpointId,
    string Name, string? Description = null, string Status = "Active")
{
    public Guid AisleId { get; } = AisleId;
    public Guid StartEndpointId { get; } = StartEndpointId;
    public Guid EndEndpointId { get; } = EndEndpointId;
    public string Name { get; } = Name;
    public string? Description { get; } = Description;
    public string Status { get; } = Status;
}
