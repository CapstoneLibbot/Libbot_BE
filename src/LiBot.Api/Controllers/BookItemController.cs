using LiBot.Domain.Library;
using Microsoft.AspNetCore.Mvc;

namespace LiBot.Api.Controllers;

[ApiController]
[Route("api/book-items")]
public sealed class BookItemController(ILibraryResourceService<BookItem> service) : LibraryResourceController<BookItem>(service) { }
