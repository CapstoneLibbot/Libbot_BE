using LiBot.Domain.Library;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LiBot.Infrastructure.Persistence;

/// <summary>
/// Applies database migrations and inserts the minimum library reference data.
/// Workflow definitions and steps are managed exclusively in the database.
/// </summary>
public static class LiBotDatabaseInitializer
{
    public static async Task InitializeLiBotDatabaseAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LiBotDbContext>();

        await db.Database.MigrateAsync(cancellationToken);
        await SeedLibraryReferenceDataAsync(db, cancellationToken);
    }

    private static async Task SeedLibraryReferenceDataAsync(
        LiBotDbContext db,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var map = await db.Maps.FirstOrDefaultAsync(
            item => item.Name == "Thư viện chính - Tầng 1",
            cancellationToken);
        if (map is null)
        {
            map = new LibraryMap(
                "Thư viện chính - Tầng 1",
                "1.0",
                30,
                20,
                "{\"unit\":\"meter\",\"origin\":[0,0],\"resolution\":0.05}");
            db.Maps.Add(map);
        }

        var endpoint = await db.Endpoints.FirstOrDefaultAsync(
            item => item.MapId == map.Id && item.Name == "Kệ A3",
            cancellationToken);
        if (endpoint is null)
        {
            endpoint = new Endpoint(map.Id, "Kệ A3", -0.8, -0.5);
            db.Endpoints.Add(endpoint);
        }

        var readingRoom = await db.Endpoints.FirstOrDefaultAsync(
            item => item.MapId == map.Id && item.Name == "Phòng đọc sách",
            cancellationToken);
        if (readingRoom is null)
        {
            readingRoom = new Endpoint(map.Id, "Phòng đọc sách", 4.07, 1.86);
            db.Endpoints.Add(readingRoom);
        }
        else
        {
            readingRoom.UpdatePosition(4.07, 1.86, readingRoom.Theta);
        }

        var floor = await db.Floors.FirstOrDefaultAsync(
            item => item.FloorNumber == 1 && item.Name == "Tầng 1",
            cancellationToken);
        if (floor is null)
        {
            floor = new Floor("Tầng 1", 1);
            db.Floors.Add(floor);
        }

        map.AssignFloor(floor.Id);

        var aisle = await db.Aisles.FirstOrDefaultAsync(
            item => item.FloorId == floor.Id && item.Code == "A",
            cancellationToken);
        if (aisle is null)
        {
            aisle = new Aisle(floor.Id, "A", "Dãy sách A", "Khu sách công nghệ");
            db.Aisles.Add(aisle);
        }

        var shelf = await db.Shelves.FirstOrDefaultAsync(
            item => item.AisleId == aisle.Id && item.Code == "A3",
            cancellationToken);
        if (shelf is null)
        {
            shelf = new Shelf(aisle.Id, "A3", "Kệ Công nghệ A3", endpoint.Id);
            db.Shelves.Add(shelf);
        }

        var shelfLevel = await db.ShelfLevels.FirstOrDefaultAsync(
            item => item.ShelfId == shelf.Id && item.LevelNumber == 2,
            cancellationToken);
        if (shelfLevel is null)
        {
            shelfLevel = new ShelfLevel(shelf.Id, 2);
            db.ShelfLevels.Add(shelfLevel);
        }

        var category = await db.BookCategories.FirstOrDefaultAsync(
            item => item.Name == "Công nghệ thông tin",
            cancellationToken);
        if (category is null)
        {
            category = new BookCategory(
                "Công nghệ thông tin",
                "Sách lập trình, robot, trí tuệ nhân tạo và hệ thống máy tính");
            db.BookCategories.Add(category);
        }

        await SeedBookAsync(
            db,
            category.Id,
            shelfLevel.Id,
            "9786043606424",
            "Lập trình Python cơ bản",
            "Nguyễn Văn An",
            "NXB Khoa học và Kỹ thuật",
            2024,
            "LIB-A3-0001",
            cancellationToken);
        await SeedBookAsync(
            db,
            category.Id,
            shelfLevel.Id,
            "9786043779555",
            "Robot tự hành và ứng dụng",
            "Trần Minh Đức",
            "NXB Bách khoa Hà Nội",
            2025,
            "LIB-A3-0002",
            cancellationToken);
        await SeedBookAsync(
            db,
            category.Id,
            shelfLevel.Id,
            "9786043923361",
            "Nhập môn trí tuệ nhân tạo",
            "Lê Hoàng Nam",
            "NXB Giáo dục Việt Nam",
            2023,
            "LIB-A3-0003",
            cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task SeedBookAsync(
        LiBotDbContext db,
        Guid categoryId,
        Guid shelfLevelId,
        string isbn,
        string title,
        string author,
        string publisher,
        int publicationYear,
        string barcode,
        CancellationToken cancellationToken)
    {
        var book = await db.Books.FirstOrDefaultAsync(item => item.Isbn == isbn, cancellationToken);
        if (book is null)
        {
            book = new Book(isbn, title, author, categoryId, publisher, publicationYear);
            db.Books.Add(book);
        }

        if (!await db.BookItems.AnyAsync(item => item.Barcode == barcode, cancellationToken))
        {
            db.BookItems.Add(new BookItem(book.Id, barcode, shelfLevelId, shelfLevelId));
        }
    }
}
