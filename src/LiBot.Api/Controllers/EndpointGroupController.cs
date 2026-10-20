using LiBot.Domain.Library;
using Microsoft.AspNetCore.Mvc;

namespace LiBot.Api.Controllers;

[ApiController]
[Route("api/endpoint-groups")]
public sealed class EndpointGroupController(ILibraryResourceService<EndpointGroup> service) : LibraryResourceController<EndpointGroup>(service) { }
