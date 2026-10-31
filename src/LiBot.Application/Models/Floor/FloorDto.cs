using LiBot.Application.Models;
using FloorEntity = LiBot.Domain.Library.Floor;

namespace LiBot.Application.Models.Floor;

public class FloorDto : LibraryResourceDto<FloorEntity>
{
    public string Name { get; set; } = string.Empty;
    public int FloorNumber { get; set; }
    public override FloorEntity ToEntity() => new(Name, FloorNumber);
}
