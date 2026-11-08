using LiBot.Domain.Enums;

namespace LiBot.Application.Models.Floor;

public class FloorResponse(Guid Id, string Name, int FloorNumber, string? Description, FloorStatus Status, DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc, IReadOnlyList<FloorAisleResponse>? Aisles = null,
    IReadOnlyList<FloorAreaResponse>? Areas = null)
{
    public Guid Id { get; } = Id;
    public string Name { get; } = Name;
    public int FloorNumber { get; } = FloorNumber;
    public string? Description { get; } = Description;
    public FloorStatus Status { get; } = Status;
    public DateTime CreatedAtUtc { get; } = CreatedAtUtc;
    public DateTime? UpdatedAtUtc { get; } = UpdatedAtUtc;
    public IReadOnlyList<FloorAisleResponse>? Aisles { get; } = Aisles;
    public IReadOnlyList<FloorAreaResponse>? Areas { get; } = Areas;
}
