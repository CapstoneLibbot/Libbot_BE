using LiBot.Domain.Library;
using Microsoft.AspNetCore.Mvc;

namespace LiBot.Api.Controllers;

/// <summary>CRUD từng bản sách vật lý, gồm barcode, trạng thái và vị trí kệ.</summary>
[ApiController]
[Route("api/book-items")]
public sealed class BookItemController(ILibraryResourceService<BookItem, BookItemDto> service) : LibraryResourceController<BookItem, BookItemDto>(service) { }
