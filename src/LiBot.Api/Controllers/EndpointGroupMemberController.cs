using LiBot.Domain.Library;
using LiBot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace LiBot.Api.Controllers;

[ApiController]
[Route("api/endpoint-group-members")]
public sealed class EndpointGroupMemberController(LiBotDbContext db) : LibraryResourceController<EndpointGroupMember>(db) { }
