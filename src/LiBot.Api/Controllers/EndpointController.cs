using LiBot.Domain.Library;
using LiBot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace LiBot.Api.Controllers;

[ApiController]
[Route("api/endpoints")]
public sealed class EndpointController(LiBotDbContext db) : LibraryResourceController<Endpoint>(db) { }
