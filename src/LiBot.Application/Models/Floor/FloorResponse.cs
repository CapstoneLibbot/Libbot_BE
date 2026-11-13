namespace LiBot.Application.Models.Floor;

public class FloorResponse(Guid Id, string Name, int FloorNumber, string? Description, string Status,
    IReadOnlyList<FloorAisleResponse>? Aisles = null)
{
    public Guid Id { get; } = Id;
    public string Name { get; } = Name;
    public int FloorNumber { get; } = FloorNumber;
    public string? Description { get; } = Description;
    public string Status { get; } = Status;
    public IReadOnlyList<FloorAisleResponse>? Aisles { get; } = Aisles;
}
