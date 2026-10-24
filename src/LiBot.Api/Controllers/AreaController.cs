using LiBot.Domain.Library;
using Microsoft.AspNetCore.Mvc;

namespace LiBot.Api.Controllers;

/// <summary>CRUD các khu vực chức năng trong thư viện.</summary>
[ApiController]
[Route("api/areas")]
public sealed class AreaController(ILibraryResourceService<Area, AreaDto> service) : LibraryResourceController<Area, AreaDto>(service) { }
