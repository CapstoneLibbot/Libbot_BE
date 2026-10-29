namespace LiBot.Application.Models.Aisle;

public class AisleWriteRequest(
    Guid FloorId,
    Guid? EndpointId,
    string Code,
    string Name,
    string? Description)
{
    public Guid FloorId { get; } = FloorId;
    public Guid? EndpointId { get; } = EndpointId;
    public string Code { get; } = Code;
    public string Name { get; } = Name;
    public string? Description { get; } = Description;
}
