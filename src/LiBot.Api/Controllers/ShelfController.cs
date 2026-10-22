using LiBot.Domain.Library;
using Microsoft.AspNetCore.Mvc;

namespace LiBot.Api.Controllers;

/// <summary>CRUD kệ sách và thông tin liên kết với lối đi.</summary>
[ApiController]
[Route("api/shelves")]
public sealed class ShelfController(ILibraryResourceService<Shelf> service) : LibraryResourceController<Shelf>(service) { }
