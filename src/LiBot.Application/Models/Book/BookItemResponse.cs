namespace LiBot.Application.Models.Book;

public class BookItemResponse(
    Guid Id,
    string Barcode,
    Guid? ExpectedShelfLevelId,
    Guid? CurrentShelfLevelId,
    DateTime? LastInventoryAtUtc,
    string Status)
{
    public Guid Id { get; } = Id;
    public string Barcode { get; } = Barcode;
    public Guid? ExpectedShelfLevelId { get; } = ExpectedShelfLevelId;
    public Guid? CurrentShelfLevelId { get; } = CurrentShelfLevelId;
    public DateTime? LastInventoryAtUtc { get; } = LastInventoryAtUtc;
    public string Status { get; } = Status;
}
