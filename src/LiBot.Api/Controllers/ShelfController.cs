using LiBot.Domain.Library;
using Microsoft.AspNetCore.Mvc;

namespace LiBot.Api.Controllers;

[ApiController]
[Route("api/shelves")]
public sealed class ShelfController(ILibraryResourceService<Shelf> service) : LibraryResourceController<Shelf>(service) { }
