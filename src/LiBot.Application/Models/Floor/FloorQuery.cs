namespace LiBot.Application.Models.Floor;

public class FloorQuery(int Page = 1, int PageSize = 20, string? Search = null,
    string SortBy = "floorNumber", string SortOrder = "asc", string? Expand = null)
{
    public int Page { get; } = Page;
    public int PageSize { get; } = PageSize;
    public string? Search { get; } = Search;
    public string SortBy { get; } = SortBy;
    public string SortOrder { get; } = SortOrder;
    public string? Expand { get; } = Expand;
}
