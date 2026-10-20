using LiBot.Domain.Library;
using Microsoft.AspNetCore.Mvc;

namespace LiBot.Api.Controllers;

[ApiController]
[Route("api/books")]
public sealed class BookController(ILibraryResourceService<Book> service) : LibraryResourceController<Book>(service) { }
