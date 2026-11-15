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

        var floor = await db.Floors.FirstOrDefaultAsync(
            item => item.FloorNumber == 1 && item.Name == "Tầng 1",
            cancellationToken);
        if (floor is null)
        {
            floor = new Floor("Tầng 1", 1);
            db.Floors.Add(floor);
        }

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
                "{\"unit\":\"meter\",\"origin\":[0,0],\"resolution\":0.05}",
                floor.Id);
            db.Maps.Add(map);
        }
        else
        {
            map.AssignFloor(floor.Id);
        }

        var endpoint = await db.Endpoints.FirstOrDefaultAsync(
            item => item.MapId == map.Id && item.Name == "Kệ A3",
            cancellationToken);
        if (endpoint is null)
        {
            endpoint = new Endpoint(map.Id, "Kệ A3", -0.8, -0.5);
            db.Endpoints.Add(endpoint);
        }

        var endpointA3End = await db.Endpoints.FirstOrDefaultAsync(item => item.MapId == map.Id && item.Name == "Kệ A3 - đầu 2", cancellationToken);
        if (endpointA3End is null) { endpointA3End = new Endpoint(map.Id, "Kệ A3 - đầu 2", -0.2, -0.5); db.Endpoints.Add(endpointA3End); }

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

        var aisle = await db.Aisles.FirstOrDefaultAsync(
            item => item.FloorId == floor.Id && item.Name == "Dãy sách A",
            cancellationToken);
        if (aisle is null)
        {
            aisle = new Aisle(floor.Id, "Dãy sách A", "Khu sách công nghệ");
            db.Aisles.Add(aisle);
        }

        var shelf = await db.Shelves.FirstOrDefaultAsync(
            item => item.AisleId == aisle.Id && item.Name == "Kệ Công nghệ A3",
            cancellationToken);
        if (shelf is null)
        {
            shelf = new Shelf(aisle.Id, endpoint.Id, endpointA3End.Id, "Kệ Công nghệ A3", "Kệ sách lập trình và AI");
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

        var levelOne = await db.ShelfLevels.FirstOrDefaultAsync(item => item.ShelfId == shelf.Id && item.LevelNumber == 1, cancellationToken);
        if (levelOne is null) { levelOne = new ShelfLevel(shelf.Id, 1, "Tầng 1 - Lập trình"); db.ShelfLevels.Add(levelOne); }
        var levelThree = await db.ShelfLevels.FirstOrDefaultAsync(item => item.ShelfId == shelf.Id && item.LevelNumber == 3, cancellationToken);
        if (levelThree is null) { levelThree = new ShelfLevel(shelf.Id, 3, "Tầng 3 - AI và Robot"); db.ShelfLevels.Add(levelThree); }

        var aisleB = await db.Aisles.FirstOrDefaultAsync(item => item.FloorId == floor.Id && item.Name == "Dãy sách B", cancellationToken);
        if (aisleB is null) { aisleB = new Aisle(floor.Id, "Dãy sách B", "Khu khoa học và kỹ năng"); db.Aisles.Add(aisleB); }
        var endpointB1 = await db.Endpoints.FirstOrDefaultAsync(item => item.MapId == map.Id && item.Name == "Kệ B1 - đầu 1", cancellationToken);
        if (endpointB1 is null) { endpointB1 = new Endpoint(map.Id, "Kệ B1 - đầu 1", 1.2, -0.5); db.Endpoints.Add(endpointB1); }
        var endpointB1End = await db.Endpoints.FirstOrDefaultAsync(item => item.MapId == map.Id && item.Name == "Kệ B1 - đầu 2", cancellationToken);
        if (endpointB1End is null) { endpointB1End = new Endpoint(map.Id, "Kệ B1 - đầu 2", 1.8, -0.5); db.Endpoints.Add(endpointB1End); }
        var shelfB = await db.Shelves.FirstOrDefaultAsync(item => item.AisleId == aisleB.Id && item.Name == "Kệ Khoa học B1", cancellationToken);
        if (shelfB is null) { shelfB = new Shelf(aisleB.Id, endpointB1.Id, endpointB1End.Id, "Kệ Khoa học B1", "Kệ khoa học phổ thông"); db.Shelves.Add(shelfB); }
        var shelfBLevel = await db.ShelfLevels.FirstOrDefaultAsync(item => item.ShelfId == shelfB.Id && item.LevelNumber == 1, cancellationToken);
        if (shelfBLevel is null) { shelfBLevel = new ShelfLevel(shelfB.Id, 1, "Tầng 1 - Khoa học"); db.ShelfLevels.Add(shelfBLevel); }
        var shelfBLevelTwo = await db.ShelfLevels.FirstOrDefaultAsync(item => item.ShelfId == shelfB.Id && item.LevelNumber == 2, cancellationToken);
        if (shelfBLevelTwo is null) { shelfBLevelTwo = new ShelfLevel(shelfB.Id, 2, "Tầng 2 - Kỹ năng"); db.ShelfLevels.Add(shelfBLevelTwo); }

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
            ["LIB-A3-0001", "LIB-A3-0004", "LIB-A3-0005"],
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
            ["LIB-A3-0002", "LIB-A3-0006", "LIB-A3-0007"],
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
            ["LIB-A3-0003", "LIB-A3-0008"],
            cancellationToken);
        await SeedBookAsync(db, category.Id, levelOne.Id, "9780132350884", "Clean Code", "Robert C. Martin", "Prentice Hall", 2008, ["LIB-A3-0101", "LIB-A3-0103", "LIB-A3-0104"], cancellationToken);
        await SeedBookAsync(db, category.Id, levelThree.Id, "9780134685991", "Effective Java", "Joshua Bloch", "Addison-Wesley", 2018, ["LIB-A3-0102", "LIB-A3-0105"], cancellationToken);
        await SeedBookAsync(db, category.Id, shelfBLevel.Id, "9786047758921", "Vũ trụ trong vỏ hạt dẻ", "Stephen Hawking", "NXB Trẻ", 2022, ["LIB-B1-0001", "LIB-B1-0002"], cancellationToken);
        await SeedBookAsync(db, category.Id, shelfBLevelTwo.Id, "9786043759915", "Tư duy phản biện", "Lê Minh Hoàng", "NXB Tổng hợp", 2024, ["LIB-B1-0003", "LIB-B1-0004", "LIB-B1-0005"], cancellationToken);

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
        IReadOnlyList<string> barcodes,
        CancellationToken cancellationToken)
    {
        var book = await db.Books.FirstOrDefaultAsync(item => item.Isbn == isbn, cancellationToken);
        if (book is null)
        {
            book = new Book(isbn, title, author, categoryId, publisher, publicationYear);
            db.Books.Add(book);
        }

        foreach (var barcode in barcodes)
            if (!await db.BookItems.AnyAsync(item => item.Barcode == barcode, cancellationToken))
                db.BookItems.Add(new BookItem(book.Id, barcode, shelfLevelId, shelfLevelId));
    }
}
