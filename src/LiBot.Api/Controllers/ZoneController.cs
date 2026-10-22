using LiBot.Domain.Library;
using Microsoft.AspNetCore.Mvc;

namespace LiBot.Api.Controllers;

/// <summary>CRUD vùng không gian trong thư viện.</summary>
[ApiController]
[Route("api/zones")]
public sealed class ZoneController(ILibraryResourceService<Zone> service) : LibraryResourceController<Zone>(service) { }
