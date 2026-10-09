using LiBot.Domain.Library;
using LiBot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace LiBot.Api.Controllers;

[ApiController]
[Route("api/shelf-levels")]
public sealed class ShelfLevelController(LiBotDbContext db) : LibraryResourceController<ShelfLevel>(db) { }
