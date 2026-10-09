using LiBot.Domain.Library;
using LiBot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace LiBot.Api.Controllers;

[ApiController]
[Route("api/areas")]
public sealed class AreaController(LiBotDbContext db) : LibraryResourceController<Area>(db) { }
