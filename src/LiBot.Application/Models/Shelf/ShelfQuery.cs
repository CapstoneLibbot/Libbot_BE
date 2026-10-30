namespace LiBot.Application.Models.Shelf;

public class ShelfQuery(int Page = 1, int PageSize = 20, string? Search = null,
    Guid? AisleId = null, string SortBy = "name", string SortOrder = "asc", string? Expand = null)
{
    public int Page { get; } = Page;
    public int PageSize { get; } = PageSize;
    public string? Search { get; } = Search;
    public Guid? AisleId { get; } = AisleId;
    public string SortBy { get; } = SortBy;
    public string SortOrder { get; } = SortOrder;
    public string? Expand { get; } = Expand;
}
