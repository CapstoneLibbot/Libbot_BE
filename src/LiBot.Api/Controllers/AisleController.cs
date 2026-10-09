using LiBot.Domain.Library;
using LiBot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace LiBot.Api.Controllers;

[ApiController]
[Route("api/aisles")]
public sealed class AisleController(LiBotDbContext db) : LibraryResourceController<Aisle>(db) { }
