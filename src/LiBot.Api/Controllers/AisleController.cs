using LiBot.Domain.Library;
using Microsoft.AspNetCore.Mvc;

namespace LiBot.Api.Controllers;

[ApiController]
[Route("api/aisles")]
public sealed class AisleController(ILibraryResourceService<Aisle> service) : LibraryResourceController<Aisle>(service) { }
