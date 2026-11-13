namespace LiBot.Application.Models.Book;

public class BookItemResponse
{
    public BookItemResponse(Guid id, string barcode, Guid? expectedShelfLevelId, Guid? currentShelfLevelId,
        DateTime? lastInventoryAtUtc, string status, DateTime createdAtUtc = default, DateTime? updatedAtUtc = null)
    {
        Id = id; Barcode = barcode; ExpectedShelfLevelId = expectedShelfLevelId; CurrentShelfLevelId = currentShelfLevelId;
        LastInventoryAtUtc = lastInventoryAtUtc; Status = status; CreatedAtUtc = createdAtUtc; UpdatedAtUtc = updatedAtUtc;
    }

    public Guid Id { get; }
    public string Barcode { get; }
    public Guid? ExpectedShelfLevelId { get; }
    public Guid? CurrentShelfLevelId { get; }
    public DateTime? LastInventoryAtUtc { get; }
    public string Status { get; }
    public DateTime CreatedAtUtc { get; }
    public DateTime? UpdatedAtUtc { get; }
}
