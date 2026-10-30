namespace LiBot.Application.Models.Aisle;

public class AisleShelfResponse(Guid Id, string Code, string Name)
{
    public Guid Id { get; } = Id;
    public string Code { get; } = Code;
    public string Name { get; } = Name;
}
