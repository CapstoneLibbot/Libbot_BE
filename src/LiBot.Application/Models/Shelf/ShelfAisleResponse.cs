namespace LiBot.Application.Models.Shelf;

public class ShelfAisleResponse(Guid Id, string Name, Guid FloorId)
{
    public Guid Id { get; } = Id;
    public string Name { get; } = Name;
    public Guid FloorId { get; } = FloorId;
}
