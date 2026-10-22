using LiBot.Domain.Library;
using Microsoft.AspNetCore.Mvc;

namespace LiBot.Api.Controllers;

/// <summary>CRUD danh mục sách; ứng dụng dùng danh mục để lọc và phân loại sách.</summary>
[ApiController]
[Route("api/book-categories")]
public sealed class BookCategoryController(ILibraryResourceService<BookCategory> service) : LibraryResourceController<BookCategory>(service) { }
