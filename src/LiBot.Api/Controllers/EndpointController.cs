using LiBot.Domain.Library;
using LiBot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using LibraryEndpoint = LiBot.Domain.Library.Endpoint;

namespace LiBot.Api.Controllers;

[ApiController]
[Route("api/endpoints")]
public sealed class EndpointController(LiBotDbContext db) : LibraryResourceController<LibraryEndpoint>(db) { }
