using LiBot.Domain.Library;
using LiBot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace LiBot.Api.Controllers;

[ApiController]
[Route("api/floors")]
public sealed class FloorController(LiBotDbContext db) : LibraryResourceController<Floor>(db) { }
