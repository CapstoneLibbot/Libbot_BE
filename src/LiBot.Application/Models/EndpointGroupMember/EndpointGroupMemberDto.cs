using LiBot.Application.Models;
using EndpointGroupMemberEntity = LiBot.Domain.Library.EndpointGroupMember;

namespace LiBot.Application.Models.EndpointGroupMember;

public class EndpointGroupMemberDto : LibraryResourceDto<EndpointGroupMemberEntity>
{
    public Guid EndpointGroupId { get; set; }
    public Guid EndpointId { get; set; }

    public override EndpointGroupMemberEntity ToEntity() => new(EndpointGroupId, EndpointId);
}
