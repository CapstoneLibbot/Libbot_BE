using LiBot.Application.Interfaces;
using LiBot.Application.Models;
using LiBot.Application.Models.Book;
using LiBot.Application.Models.BookCategory;
using LiBot.Application.Models.BookItem;
using LiBot.Domain.Library;
using Microsoft.AspNetCore.Mvc;
using BookCategoryResourceDto = LiBot.Application.Models.BookCategoryDto;
using BookItemResourceDto = LiBot.Application.Models.BookItemDto;

namespace LiBot.Api.Controllers;

/// <summary>CRUD sách, danh mục sách và các bản sách vật lý trong thư viện.</summary>
[ApiController]
[Route("api/books")]
public class BookController : ControllerBase
{
    private readonly IBookService _bookService;
    private readonly ILibraryResourceService<BookCategory, BookCategoryResourceDto> _categoryService;
    private readonly ILibraryResourceService<BookItem, BookItemResourceDto> _bookItemService;

    public BookController(
        IBookService bookService,
        ILibraryResourceService<BookCategory, BookCategoryResourceDto> categoryService,
        ILibraryResourceService<BookItem, BookItemResourceDto> bookItemService)
    {
        _bookService = bookService ?? throw new ArgumentNullException(nameof(bookService));
        _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
        _bookItemService = bookItemService ?? throw new ArgumentNullException(nameof(bookItemService));
    }

    [HttpGet]
    public async Task<ActionResult<BaseResponse<BookListResponse>>> GetBooks(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] string sortBy = "title",
        [FromQuery] string sortOrder = "asc",
        [FromQuery] string? expand = null,
        CancellationToken cancellationToken = default)
    {
        if (page < 1 || pageSize is < 1 or > 100 || (long)(page - 1) * pageSize > int.MaxValue)
            return BadRequest(Error("page must be at least 1 and pageSize must be between 1 and 100."));
        if (!new[] { "title", "author", "isbn", "publicationyear" }
                .Contains(sortBy.ToLowerInvariant()))
            return BadRequest(Error("sortBy must be title, author, isbn or publicationYear."));
        if (!new[] { "asc", "desc" }.Contains(sortOrder.ToLowerInvariant()))
            return BadRequest(Error("sortOrder must be asc or desc."));
        if (!HasValidExpansion(expand, "category", "items"))
            return BadRequest(Error("expand supports category and items."));

        var response = await _bookService.GetAllAsync(
            new BookQuery(page, pageSize, search, categoryId, sortBy, sortOrder, expand), cancellationToken);
        return Ok(new BaseResponse<BookListResponse>(response));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BaseResponse<BookResponse>>> GetBookById(
        Guid id,
        [FromQuery] string? expand,
        CancellationToken cancellationToken)
    {
        if (!HasValidExpansion(expand, "category", "items"))
            return BadRequest(Error("expand supports category and items."));
        var book = await _bookService.GetByIdAsync(id, expand, cancellationToken);
        return book is null ? NotFound(Error("Book was not found.")) : Ok(new BaseResponse<BookResponse>(book));
    }

    [HttpPost]
    public async Task<ActionResult<BaseResponse<BookResponse>>> CreateBook(
        [FromBody] BookWriteRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var book = await _bookService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetBookById), new { id = book.Id }, new BaseResponse<BookResponse>(book));
        }
        catch (ArgumentException exception) { return BadRequest(Error(exception.Message)); }
        catch (KeyNotFoundException exception) { return NotFound(Error(exception.Message)); }
        catch (InvalidOperationException exception) { return Conflict(Error(exception.Message)); }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<BaseResponse<BookResponse>>> UpdateBook(
        Guid id,
        [FromBody] BookWriteRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var book = await _bookService.UpdateAsync(id, request, cancellationToken);
            return book is null ? NotFound(Error("Book was not found.")) : Ok(new BaseResponse<BookResponse>(book));
        }
        catch (ArgumentException exception) { return BadRequest(Error(exception.Message)); }
        catch (KeyNotFoundException exception) { return NotFound(Error(exception.Message)); }
        catch (InvalidOperationException exception) { return Conflict(Error(exception.Message)); }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteBook(Guid id, CancellationToken cancellationToken)
    {
        if (!await _bookService.DeleteAsync(id, cancellationToken)) return NotFound(Error("Book was not found."));
        return Ok(BaseResponse<object>.SuccessMessage("Book deleted successfully."));
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken) =>
        Ok(new BaseResponse<IReadOnlyList<BookCategoryResourceDto>>(
            await _categoryService.GetAllAsync(cancellationToken)));

    [HttpGet("categories/{id:guid}")]
    public async Task<IActionResult> GetCategoryById(Guid id, CancellationToken cancellationToken)
    {
        var category = await _categoryService.GetByIdAsync(id, cancellationToken);
        return category is null ? NotFound(Error("Book category was not found.")) : Ok(new BaseResponse<BookCategoryResourceDto>(category));
    }

    [HttpPost("categories")]
    public async Task<IActionResult> CreateCategory([FromBody] BookCategoryResourceDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var category = await _categoryService.CreateAsync(dto, cancellationToken);
            return Created($"/api/books/categories/{category.Id}", new BaseResponse<BookCategoryResourceDto>(category));
        }
        catch (ArgumentException exception) { return BadRequest(Error(exception.Message)); }
    }

    [HttpPut("categories/{id:guid}")]
    public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] BookCategoryResourceDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var category = await _categoryService.UpdateAsync(id, dto, cancellationToken);
            return category is null ? NotFound(Error("Book category was not found.")) : Ok(new BaseResponse<BookCategoryResourceDto>(category));
        }
        catch (ArgumentException exception) { return BadRequest(Error(exception.Message)); }
    }

    [HttpDelete("categories/{id:guid}")]
    public async Task<IActionResult> DeleteCategory(Guid id, CancellationToken cancellationToken) =>
        await _categoryService.DeleteAsync(id, cancellationToken) ? Ok(BaseResponse<object>.SuccessMessage("Book category deleted successfully.")) : NotFound(Error("Book category was not found."));

    [HttpGet("items")]
    public async Task<IActionResult> GetBookItems(CancellationToken cancellationToken) =>
        Ok(new BaseResponse<IReadOnlyList<BookItemResourceDto>>(
            await _bookItemService.GetAllAsync(cancellationToken)));

    [HttpGet("items/{id:guid}")]
    public async Task<IActionResult> GetBookItemById(Guid id, CancellationToken cancellationToken)
    {
        var item = await _bookItemService.GetByIdAsync(id, cancellationToken);
        return item is null ? NotFound(Error("Book item was not found.")) : Ok(new BaseResponse<BookItemResourceDto>(item));
    }

    [HttpPost("items")]
    public async Task<IActionResult> CreateBookItem([FromBody] BookItemResourceDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var item = await _bookItemService.CreateAsync(dto, cancellationToken);
            return Created($"/api/books/items/{item.Id}", new BaseResponse<BookItemResourceDto>(item));
        }
        catch (ArgumentException exception) { return BadRequest(Error(exception.Message)); }
    }

    [HttpPut("items/{id:guid}")]
    public async Task<IActionResult> UpdateBookItem(Guid id, [FromBody] BookItemResourceDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var item = await _bookItemService.UpdateAsync(id, dto, cancellationToken);
            return item is null ? NotFound(Error("Book item was not found.")) : Ok(new BaseResponse<BookItemResourceDto>(item));
        }
        catch (ArgumentException exception) { return BadRequest(Error(exception.Message)); }
    }

    [HttpDelete("items/{id:guid}")]
    public async Task<IActionResult> DeleteBookItem(Guid id, CancellationToken cancellationToken) =>
        await _bookItemService.DeleteAsync(id, cancellationToken) ? Ok(BaseResponse<object>.SuccessMessage("Book item deleted successfully.")) : NotFound(Error("Book item was not found."));

    private static bool HasValidExpansion(string? expand, params string[] supported)
    {
        if (string.IsNullOrWhiteSpace(expand)) return true;
        var values = expand.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return values.All(value => supported.Contains(value, StringComparer.OrdinalIgnoreCase));
    }

    private static BaseResponse<object> Error(string message) => BaseResponse<object>.Failure(message);
}
