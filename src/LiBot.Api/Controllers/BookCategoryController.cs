using LiBot.Domain.Library;
using LiBot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace LiBot.Api.Controllers;

[ApiController]
[Route("api/book-categories")]
public sealed class BookCategoryController(LiBotDbContext db) : LibraryResourceController<BookCategory>(db) { }
