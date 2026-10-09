using System.Reflection;
using System.Text.Json;
using LiBot.Domain.Library;
using LiBot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LiBot.Api.Controllers;

[ApiController]
[Route("api/library")]
public class LibraryController(LiBotDbContext db) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [HttpGet("{resource}")]
    public Task<IActionResult> GetAll(string resource, CancellationToken cancellationToken) => resource switch
    {
        "books" => List<Book>(cancellationToken),
        "book-categories" => List<BookCategory>(cancellationToken),
        "book-items" => List<BookItem>(cancellationToken),
        "maps" => List<LibraryMap>(cancellationToken),
        "floors" => List<Floor>(cancellationToken),
        "zones" => List<Zone>(cancellationToken),
        "areas" => List<Area>(cancellationToken),
        "aisles" => List<Aisle>(cancellationToken),
        "shelves" => List<Shelf>(cancellationToken),
        "shelf-levels" => List<ShelfLevel>(cancellationToken),
        "endpoints" => List<Endpoint>(cancellationToken),
        "endpoint-groups" => List<EndpointGroup>(cancellationToken),
        "endpoint-group-members" => List<EndpointGroupMember>(cancellationToken),
        _ => Task.FromResult<IActionResult>(NotFound("Unknown library resource."))
    };

    [HttpGet("{resource}/{id:guid}")]
    public Task<IActionResult> GetById(string resource, Guid id, CancellationToken cancellationToken) => resource switch
    {
        "books" => Find<Book>(id, cancellationToken),
        "book-categories" => Find<BookCategory>(id, cancellationToken),
        "book-items" => Find<BookItem>(id, cancellationToken),
        "maps" => Find<LibraryMap>(id, cancellationToken),
        "floors" => Find<Floor>(id, cancellationToken),
        "zones" => Find<Zone>(id, cancellationToken),
        "areas" => Find<Area>(id, cancellationToken),
        "aisles" => Find<Aisle>(id, cancellationToken),
        "shelves" => Find<Shelf>(id, cancellationToken),
        "shelf-levels" => Find<ShelfLevel>(id, cancellationToken),
        "endpoints" => Find<Endpoint>(id, cancellationToken),
        "endpoint-groups" => Find<EndpointGroup>(id, cancellationToken),
        "endpoint-group-members" => Find<EndpointGroupMember>(id, cancellationToken),
        _ => Task.FromResult<IActionResult>(NotFound("Unknown library resource."))
    };

    [HttpPost("{resource}")]
    public Task<IActionResult> Create(string resource, [FromBody] JsonElement body, CancellationToken cancellationToken) => resource switch
    {
        "books" => Create<Book>(body, cancellationToken),
        "book-categories" => Create<BookCategory>(body, cancellationToken),
        "book-items" => Create<BookItem>(body, cancellationToken),
        "maps" => Create<LibraryMap>(body, cancellationToken),
        "floors" => Create<Floor>(body, cancellationToken),
        "zones" => Create<Zone>(body, cancellationToken),
        "areas" => Create<Area>(body, cancellationToken),
        "aisles" => Create<Aisle>(body, cancellationToken),
        "shelves" => Create<Shelf>(body, cancellationToken),
        "shelf-levels" => Create<ShelfLevel>(body, cancellationToken),
        "endpoints" => Create<Endpoint>(body, cancellationToken),
        "endpoint-groups" => Create<EndpointGroup>(body, cancellationToken),
        "endpoint-group-members" => Create<EndpointGroupMember>(body, cancellationToken),
        _ => Task.FromResult<IActionResult>(NotFound("Unknown library resource."))
    };

    [HttpPut("{resource}/{id:guid}")]
    public Task<IActionResult> Update(string resource, Guid id, [FromBody] JsonElement body, CancellationToken cancellationToken) => resource switch
    {
        "books" => Replace<Book>(id, body, cancellationToken),
        "book-categories" => Replace<BookCategory>(id, body, cancellationToken),
        "book-items" => Replace<BookItem>(id, body, cancellationToken),
        "maps" => Replace<LibraryMap>(id, body, cancellationToken),
        "floors" => Replace<Floor>(id, body, cancellationToken),
        "zones" => Replace<Zone>(id, body, cancellationToken),
        "areas" => Replace<Area>(id, body, cancellationToken),
        "aisles" => Replace<Aisle>(id, body, cancellationToken),
        "shelves" => Replace<Shelf>(id, body, cancellationToken),
        "shelf-levels" => Replace<ShelfLevel>(id, body, cancellationToken),
        "endpoints" => Replace<Endpoint>(id, body, cancellationToken),
        "endpoint-groups" => Replace<EndpointGroup>(id, body, cancellationToken),
        "endpoint-group-members" => Replace<EndpointGroupMember>(id, body, cancellationToken),
        _ => Task.FromResult<IActionResult>(NotFound("Unknown library resource."))
    };

    [HttpDelete("{resource}/{id:guid}")]
    public Task<IActionResult> Delete(string resource, Guid id, CancellationToken cancellationToken) => resource switch
    {
        "books" => Remove<Book>(id, cancellationToken),
        "book-categories" => Remove<BookCategory>(id, cancellationToken),
        "book-items" => Remove<BookItem>(id, cancellationToken),
        "maps" => Remove<LibraryMap>(id, cancellationToken),
        "floors" => Remove<Floor>(id, cancellationToken),
        "zones" => Remove<Zone>(id, cancellationToken),
        "areas" => Remove<Area>(id, cancellationToken),
        "aisles" => Remove<Aisle>(id, cancellationToken),
        "shelves" => Remove<Shelf>(id, cancellationToken),
        "shelf-levels" => Remove<ShelfLevel>(id, cancellationToken),
        "endpoints" => Remove<Endpoint>(id, cancellationToken),
        "endpoint-groups" => Remove<EndpointGroup>(id, cancellationToken),
        "endpoint-group-members" => Remove<EndpointGroupMember>(id, cancellationToken),
        _ => Task.FromResult<IActionResult>(NotFound("Unknown library resource."))
    };

    private async Task<IActionResult> List<TEntity>(CancellationToken cancellationToken) where TEntity : class =>
        Ok(await db.Set<TEntity>().AsNoTracking().ToListAsync(cancellationToken));

    private async Task<IActionResult> Find<TEntity>(Guid id, CancellationToken cancellationToken) where TEntity : class
    {
        var entity = await db.Set<TEntity>().FindAsync([id], cancellationToken);
        return entity is null ? NotFound() : Ok(entity);
    }

    private async Task<IActionResult> Create<TEntity>(JsonElement body, CancellationToken cancellationToken) where TEntity : class
    {
        var entity = Deserialize<TEntity>(body);
        if (entity is null) return BadRequest("Invalid request body.");
        db.Set<TEntity>().Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetById), new { resource = ResourceName<TEntity>(), id = ReadId(entity) }, entity);
    }

    private async Task<IActionResult> Replace<TEntity>(Guid id, JsonElement body, CancellationToken cancellationToken) where TEntity : class
    {
        var current = await db.Set<TEntity>().FindAsync([id], cancellationToken);
        if (current is null) return NotFound();
        var replacement = Deserialize<TEntity>(body);
        if (replacement is null) return BadRequest("Invalid request body.");
        foreach (var property in typeof(TEntity).GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (property.Name is "Id" or "CreatedAtUtc" or "UpdatedAtUtc" || property.GetSetMethod(true) is null) continue;
            property.SetValue(current, property.GetValue(replacement));
        }
        await db.SaveChangesAsync(cancellationToken);
        return Ok(current);
    }

    private async Task<IActionResult> Remove<TEntity>(Guid id, CancellationToken cancellationToken) where TEntity : class
    {
        var entity = await db.Set<TEntity>().FindAsync([id], cancellationToken);
        if (entity is null) return NotFound();
        db.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static TEntity? Deserialize<TEntity>(JsonElement body) where TEntity : class
    {
        try { return body.Deserialize<TEntity>(JsonOptions); }
        catch (JsonException) { return null; }
    }

    private static object? ReadId<TEntity>(TEntity entity) => typeof(TEntity).GetProperty("Id")?.GetValue(entity);
    private static string ResourceName<TEntity>() => typeof(TEntity) switch
    {
        var type when type == typeof(Book) => "books",
        var type when type == typeof(BookCategory) => "book-categories",
        var type when type == typeof(BookItem) => "book-items",
        var type when type == typeof(LibraryMap) => "maps",
        var type when type == typeof(Floor) => "floors",
        var type when type == typeof(Zone) => "zones",
        var type when type == typeof(Area) => "areas",
        var type when type == typeof(Aisle) => "aisles",
        var type when type == typeof(Shelf) => "shelves",
        var type when type == typeof(ShelfLevel) => "shelf-levels",
        var type when type == typeof(Endpoint) => "endpoints",
        var type when type == typeof(EndpointGroup) => "endpoint-groups",
        _ => "endpoint-group-members"
    };
}
