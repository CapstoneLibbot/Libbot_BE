namespace LiBot.Application.Models.Floor;

public class FloorAisleResponse(Guid Id, string Name)
{
    public Guid Id { get; } = Id;
    public string Name { get; } = Name;
}
