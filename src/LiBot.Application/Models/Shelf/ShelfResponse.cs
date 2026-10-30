using LiBot.Application.Models.ShelfLevel;

namespace LiBot.Application.Models.Shelf;

public class ShelfResponse(Guid Id, Guid AisleId, string Code, string Name,
    DateTime CreatedAtUtc, DateTime? UpdatedAtUtc, ShelfAisleResponse? Aisle = null,
    IReadOnlyList<ShelfLevelResponse>? Levels = null)
{
    public Guid Id { get; } = Id;
    public Guid AisleId { get; } = AisleId;
    public string Code { get; } = Code;
    public string Name { get; } = Name;
    public DateTime CreatedAtUtc { get; } = CreatedAtUtc;
    public DateTime? UpdatedAtUtc { get; } = UpdatedAtUtc;
    public ShelfAisleResponse? Aisle { get; } = Aisle;
    public IReadOnlyList<ShelfLevelResponse>? Levels { get; } = Levels;
}
