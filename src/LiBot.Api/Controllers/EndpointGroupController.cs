using LiBot.Domain.Library;
using LiBot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace LiBot.Api.Controllers;

[ApiController]
[Route("api/endpoint-groups")]
public sealed class EndpointGroupController(LiBotDbContext db) : LibraryResourceController<EndpointGroup>(db) { }
