using LiBot.Domain.Library;
using Microsoft.AspNetCore.Mvc;

namespace LiBot.Api.Controllers;

[ApiController]
[Route("api/areas")]
public sealed class AreaController(ILibraryResourceService<Area> service) : LibraryResourceController<Area>(service) { }
