using LiBot.Domain.Library;
using Microsoft.AspNetCore.Mvc;

namespace LiBot.Api.Controllers;

/// <summary>CRUD lối đi trong thư viện và điểm điều hướng gắn với lối đi.</summary>
[ApiController]
[Route("api/aisles")]
public sealed class AisleController(ILibraryResourceService<Aisle, AisleDto> service) : LibraryResourceController<Aisle, AisleDto>(service) { }
