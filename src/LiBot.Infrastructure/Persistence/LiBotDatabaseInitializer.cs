using System.Text.Json;
using LiBot.Contracts.Workflow;
using LiBot.Domain.Library;
using LiBot.Domain.Workflow;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LiBot.Infrastructure.Persistence;

/// <summary>
/// Creates/updates the LiBot database and inserts the minimum reference data
/// required by flow 1 (search for a book and guide the reader to its shelf).
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
        await SeedFlowOneAsync(db, cancellationToken);
    }

    internal static async Task SeedFlowOneAsync(
        LiBotDbContext db,
        CancellationToken cancellationToken = default)
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
            endpoint = new Endpoint(map.Id, "Kệ A3", -0.37, -0.01);
            db.Endpoints.Add(endpoint);
        }

        var floor = await db.Floors.FirstOrDefaultAsync(
            item => item.FloorNumber == 1 && item.Name == "Tầng 1",
            cancellationToken);
        if (floor is null)
        {
            floor = new Floor("Tầng 1", 1);
            db.Floors.Add(floor);
        }

        var aisle = await db.Aisles.FirstOrDefaultAsync(
            item => item.FloorId == floor.Id && item.Code == "A",
            cancellationToken);
        if (aisle is null)
        {
            aisle = new Aisle(floor.Id, "A", "Dãy sách A", endpoint.Id, "Khu sách công nghệ");
            db.Aisles.Add(aisle);
        }

        var shelf = await db.Shelves.FirstOrDefaultAsync(
            item => item.AisleId == aisle.Id && item.Code == "A3",
            cancellationToken);
        if (shelf is null)
        {
            shelf = new Shelf(aisle.Id, "A3", "Kệ Công nghệ A3");
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

        await SeedFlowOneWorkflowAsync(db, cancellationToken);
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

    private static async Task SeedFlowOneWorkflowAsync(
        LiBotDbContext db,
        CancellationToken cancellationToken)
    {
        var workflow = await db.Workflows.FirstOrDefaultAsync(
            item => item.Type == BookGuidanceWorkflow.Type,
            cancellationToken);
        if (workflow is null)
        {
            workflow = new WorkflowDefinition(
                "Tìm và hướng dẫn đến sách",
                "Luồng 1: tìm sách, hiển thị vị trí và dẫn người đọc đến đúng kệ.");
            workflow.SetType(BookGuidanceWorkflow.Type);
            db.Workflows.Add(workflow);
        }

        var existingTaskNames = (await db.WorkflowTasks
            .Where(item => item.WorkflowId == workflow.Id)
            .Select(item => item.Name)
            .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.Ordinal);

        var steps = BookGuidanceWorkflow.CreateSteps();
        for (var index = 0; index < steps.Count; index++)
        {
            var step = steps[index];
            var name = (string)step["name"]!;
            if (existingTaskNames.Contains(name)) continue;

            var task = new WorkflowTask(workflow.Id, index + 1, name);
            db.WorkflowTasks.Add(task);
            db.TaskSteps.Add(new TaskStep(
                task.Id,
                (string)step["type"]!,
                JsonSerializer.Serialize(step)));
        }
    }
}
