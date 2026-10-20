using LiBot.Domain.Library;
using Microsoft.AspNetCore.Mvc;

namespace LiBot.Api.Controllers;

[ApiController]
[Route("api/shelf-levels")]
public sealed class ShelfLevelController(ILibraryResourceService<ShelfLevel> service) : LibraryResourceController<ShelfLevel>(service) { }
