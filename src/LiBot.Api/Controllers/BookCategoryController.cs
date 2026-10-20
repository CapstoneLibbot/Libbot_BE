using LiBot.Domain.Library;
using Microsoft.AspNetCore.Mvc;

namespace LiBot.Api.Controllers;

[ApiController]
[Route("api/book-categories")]
public sealed class BookCategoryController(ILibraryResourceService<BookCategory> service) : LibraryResourceController<BookCategory>(service) { }
