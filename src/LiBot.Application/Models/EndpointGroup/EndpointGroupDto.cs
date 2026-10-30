using LiBot.Application.Models;
using EndpointGroupEntity = LiBot.Domain.Library.EndpointGroup;

namespace LiBot.Application.Models.EndpointGroup;

public class EndpointGroupDto : LibraryResourceDto<EndpointGroupEntity>
{
    public string Name { get; set; } = string.Empty;

    public override EndpointGroupEntity ToEntity() => new(Name);
}
