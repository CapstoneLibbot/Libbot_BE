namespace LiBot.Application.Models.Endpoint;

public class EndpointListResponse(
    IReadOnlyList<EndpointResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    string SortBy,
    string SortOrder)
{
    public IReadOnlyList<EndpointResponse> Items { get; } = Items;
    public int Page { get; } = Page;
    public int PageSize { get; } = PageSize;
    public int TotalCount { get; } = TotalCount;
    public int TotalPages { get; } = TotalPages;
    public string SortBy { get; } = SortBy;
    public string SortOrder { get; } = SortOrder;
}
