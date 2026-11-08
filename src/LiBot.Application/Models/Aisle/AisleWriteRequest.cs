using LiBot.Domain.Enums;

namespace LiBot.Application.Models.Aisle;

public class AisleWriteRequest(
    Guid FloorId,
    string Name,
    string? Description,
    AisleStatus? Status = null)
{
    public Guid FloorId { get; } = FloorId;
    public string Name { get; } = Name;
    public string? Description { get; } = Description;
    public AisleStatus? Status { get; } = Status;
}
