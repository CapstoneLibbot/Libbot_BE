namespace LiBot.Application.Models.Book;

public class BookQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    Guid? CategoryId = null,
    string SortBy = "title",
    string SortOrder = "asc",
    string? Expand = null)
{
    public int Page { get; } = Page;
    public int PageSize { get; } = PageSize;
    public string? Search { get; } = Search;
    public Guid? CategoryId { get; } = CategoryId;
    public string SortBy { get; } = SortBy;
    public string SortOrder { get; } = SortOrder;
    public string? Expand { get; } = Expand;
}
