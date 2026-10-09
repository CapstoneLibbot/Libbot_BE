using LiBot.Domain.Library;
using LiBot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace LiBot.Api.Controllers;

[ApiController]
[Route("api/zones")]
public sealed class ZoneController(LiBotDbContext db) : LibraryResourceController<Zone>(db) { }
