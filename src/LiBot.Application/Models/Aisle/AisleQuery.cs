namespace LiBot.Application.Models.Aisle;

public class AisleQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    Guid? FloorId = null,
    string SortBy = "name",
    string SortOrder = "asc",
    string? Expand = null,
    string? Status = null)
{
    public int Page { get; } = Page;
    public int PageSize { get; } = PageSize;
    public string? Search { get; } = Search;
    public Guid? FloorId { get; } = FloorId;
    public string SortBy { get; } = SortBy;
    public string SortOrder { get; } = SortOrder;
    public string? Expand { get; } = Expand;
    public string? Status { get; } = Status;
}
