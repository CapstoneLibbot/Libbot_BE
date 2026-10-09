using LiBot.Domain.Library;
using LiBot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace LiBot.Api.Controllers;

[ApiController]
[Route("api/book-items")]
public sealed class BookItemController(LiBotDbContext db) : LibraryResourceController<BookItem>(db) { }
