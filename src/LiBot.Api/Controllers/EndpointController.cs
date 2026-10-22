using LiBot.Domain.Library;
using Microsoft.AspNetCore.Mvc;
using LibraryEndpoint = LiBot.Domain.Library.Endpoint;

namespace LiBot.Api.Controllers;

/// <summary>CRUD điểm đến điều hướng của robot trong các bản đồ.</summary>
[ApiController]
[Route("api/endpoints")]
public sealed class EndpointController(ILibraryResourceService<LibraryEndpoint> service) : LibraryResourceController<LibraryEndpoint>(service) { }
