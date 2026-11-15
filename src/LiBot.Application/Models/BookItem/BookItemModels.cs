namespace LiBot.Application.Models.BookItem;

public sealed record BookItemQuery(int Page = 1, int PageSize = 20, string? Barcode = null, Guid? BookId = null, string? Status = null, Guid? ShelfLevelId = null);
public sealed record BookItemWriteRequest(Guid BookId, string Barcode, Guid? ExpectedShelfLevelId, Guid? CurrentShelfLevelId, string Status = "AVAILABLE");
public sealed record BookItemDetailResponse(Guid Id, Guid BookId, string Barcode, string Status, DateTime? LastInventoryAtUtc, Guid? ExpectedShelfLevelId, Guid? CurrentShelfLevelId);
public sealed record BookItemListResponse(IReadOnlyList<BookItemDetailResponse> Items, int Page, int PageSize, int TotalCount, int TotalPages);
