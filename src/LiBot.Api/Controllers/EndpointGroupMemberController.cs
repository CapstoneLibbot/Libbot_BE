using LiBot.Domain.Library;
using Microsoft.AspNetCore.Mvc;

namespace LiBot.Api.Controllers;

/// <summary>CRUD quan hệ thành viên giữa nhóm điểm đến và endpoint.</summary>
[ApiController]
[Route("api/endpoint-group-members")]
public sealed class EndpointGroupMemberController(ILibraryResourceService<EndpointGroupMember> service) : LibraryResourceController<EndpointGroupMember>(service) { }
