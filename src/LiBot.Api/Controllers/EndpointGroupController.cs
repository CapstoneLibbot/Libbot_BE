using LiBot.Domain.Library;
using Microsoft.AspNetCore.Mvc;

namespace LiBot.Api.Controllers;

/// <summary>CRUD nhóm các điểm điều hướng để gom và quản lý điểm đến.</summary>
[ApiController]
[Route("api/endpoint-groups")]
public sealed class EndpointGroupController(ILibraryResourceService<EndpointGroup, EndpointGroupDto> service) : LibraryResourceController<EndpointGroup, EndpointGroupDto>(service) { }
