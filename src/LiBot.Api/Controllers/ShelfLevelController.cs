using LiBot.Domain.Library;
using Microsoft.AspNetCore.Mvc;

namespace LiBot.Api.Controllers;

/// <summary>CRUD các tầng/đợt trên kệ dùng để lưu vị trí sách.</summary>
[ApiController]
[Route("api/shelf-levels")]
public sealed class ShelfLevelController(ILibraryResourceService<ShelfLevel> service) : LibraryResourceController<ShelfLevel>(service) { }
