using LiBot.Domain.Library;
using Microsoft.AspNetCore.Mvc;

namespace LiBot.Api.Controllers;

/// <summary>CRUD đầu sách trong danh mục thư viện.</summary>
[ApiController]
[Route("api/books")]
public sealed class BookController(ILibraryResourceService<Book> service) : LibraryResourceController<Book>(service) { }
