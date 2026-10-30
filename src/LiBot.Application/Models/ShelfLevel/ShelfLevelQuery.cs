namespace LiBot.Application.Models.ShelfLevel;

public class ShelfLevelQuery(int Page = 1, int PageSize = 20, Guid? ShelfId = null,
    string SortBy = "levelNumber", string SortOrder = "asc")
{
    public int Page { get; } = Page;
    public int PageSize { get; } = PageSize;
    public Guid? ShelfId { get; } = ShelfId;
    public string SortBy { get; } = SortBy;
    public string SortOrder { get; } = SortOrder;
}
