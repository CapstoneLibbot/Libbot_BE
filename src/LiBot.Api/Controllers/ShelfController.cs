using LiBot.Domain.Library;
using LiBot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace LiBot.Api.Controllers;

[ApiController]
[Route("api/shelves")]
public sealed class ShelfController(LiBotDbContext db) : LibraryResourceController<Shelf>(db) { }
