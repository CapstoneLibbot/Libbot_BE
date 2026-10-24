using LiBot.Domain.Library;
using Microsoft.AspNetCore.Mvc;

namespace LiBot.Api.Controllers;

/// <summary>CRUD tầng của thư viện.</summary>
[ApiController]
[Route("api/floors")]
public sealed class FloorController(ILibraryResourceService<Floor, FloorDto> service) : LibraryResourceController<Floor, FloorDto>(service) { }
