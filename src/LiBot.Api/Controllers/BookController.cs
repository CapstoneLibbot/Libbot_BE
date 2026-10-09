using LiBot.Domain.Library;
using LiBot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace LiBot.Api.Controllers;

[ApiController]
[Route("api/books")]
public sealed class BookController(LiBotDbContext db) : LibraryResourceController<Book>(db) { }
