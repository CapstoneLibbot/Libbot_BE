using LiBot.Domain.Library;
using Microsoft.AspNetCore.Mvc;

namespace LiBot.Api.Controllers;

[ApiController]
[Route("api/zones")]
public sealed class ZoneController(ILibraryResourceService<Zone> service) : LibraryResourceController<Zone>(service) { }
