using LiBot.Domain.Common;

namespace LiBot.Domain.Inventory;

public sealed class BookReceiptDetail : Entity
{
    public Guid ReceiptId { get; private set; }
    public Guid BookItemId { get; private set; }
    public Guid BookId { get; private set; }
    public string? Note { get; private set; }

    public BookReceiptDetail(Guid receiptId, Guid bookItemId, Guid bookId, string? note = null)
    {
        ReceiptId = Required(receiptId, nameof(receiptId));
        BookItemId = Required(bookItemId, nameof(bookItemId));
        BookId = Required(bookId, nameof(bookId));
        Note = note?.Trim();
    }

    private static Guid Required(Guid value, string name) =>
        value == Guid.Empty
            ? throw new ArgumentException("Identifier is required.", name)
            : value;
}
