namespace LiBot.Application.Models.Floor;

public class FloorAisleResponse(Guid Id, string Code, string Name)
{
    public Guid Id { get; } = Id;
    public string Code { get; } = Code;
    public string Name { get; } = Name;
}
