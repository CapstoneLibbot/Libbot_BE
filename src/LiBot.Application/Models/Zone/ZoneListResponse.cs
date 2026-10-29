namespace LiBot.Application.Models.Zone;

public class ZoneListResponse(IReadOnlyList<ZoneResponse> Items, int Page, int PageSize,
    int TotalCount, int TotalPages, string SortBy, string SortOrder)
{
    public IReadOnlyList<ZoneResponse> Items { get; } = Items;
    public int Page { get; } = Page;
    public int PageSize { get; } = PageSize;
    public int TotalCount { get; } = TotalCount;
    public int TotalPages { get; } = TotalPages;
    public string SortBy { get; } = SortBy;
    public string SortOrder { get; } = SortOrder;
}
