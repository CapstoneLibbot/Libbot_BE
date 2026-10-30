namespace LiBot.Application.Models.Floor;

public class FloorAreaResponse(Guid Id, string Name, string? Description)
{
    public Guid Id { get; } = Id;
    public string Name { get; } = Name;
    public string? Description { get; } = Description;
}
