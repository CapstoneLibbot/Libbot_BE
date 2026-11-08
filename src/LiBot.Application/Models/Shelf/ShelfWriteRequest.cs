namespace LiBot.Application.Models.Shelf;

public class ShelfWriteRequest(Guid AisleId, string Code, string Name, Guid? EndpointId = null,
    string? Description = null, string Status = "Active")
{
    public Guid AisleId { get; } = AisleId;
    public string Code { get; } = Code;
    public string Name { get; } = Name;
    public Guid? EndpointId { get; } = EndpointId;
    public string? Description { get; } = Description;
    public string Status { get; } = Status;
}
