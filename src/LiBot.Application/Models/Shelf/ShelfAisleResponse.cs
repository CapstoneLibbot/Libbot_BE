namespace LiBot.Application.Models.Shelf;

public class ShelfAisleResponse(Guid Id, string Code, string Name, Guid FloorId)
{
    public Guid Id { get; } = Id;
    public string Code { get; } = Code;
    public string Name { get; } = Name;
    public Guid FloorId { get; } = FloorId;
}
