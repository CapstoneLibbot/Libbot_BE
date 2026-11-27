namespace LiBot.Application.Models.Aisle;

public class AisleShelfResponse(Guid Id, string Name)
{
    public Guid Id { get; } = Id;
    public string Name { get; } = Name;
}
