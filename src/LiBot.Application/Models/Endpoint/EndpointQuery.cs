namespace LiBot.Application.Models.Endpoint;

public class EndpointQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    Guid? MapId = null,
    bool? IsActive = null,
    string SortBy = "name",
    string SortOrder = "asc",
    string? Expand = null)
{
    public int Page { get; } = Page;
    public int PageSize { get; } = PageSize;
    public string? Search { get; } = Search;
    public Guid? MapId { get; } = MapId;
    public bool? IsActive { get; } = IsActive;
    public string SortBy { get; } = SortBy;
    public string SortOrder { get; } = SortOrder;
    public string? Expand { get; } = Expand;
}
