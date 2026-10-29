namespace LiBot.Application.Models.Shelf;

public class ShelfWriteRequest(Guid AisleId, string Code, string Name)
{
    public Guid AisleId { get; } = AisleId;
    public string Code { get; } = Code;
    public string Name { get; } = Name;
}
