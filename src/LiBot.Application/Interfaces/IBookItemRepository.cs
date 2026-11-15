using LiBot.Application.Models.BookItem;
namespace LiBot.Application.Interfaces;
public interface IBookItemRepository
{
    Task<(IReadOnlyList<BookItemDetailResponse> Items, int Total)> SearchAsync(BookItemQuery query, CancellationToken cancellationToken);
    Task<BookItemDetailResponse?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> BookExistsAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> ShelfLevelExistsAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> BarcodeExistsAsync(string barcode, Guid? exceptId, CancellationToken cancellationToken);
    Task<bool> HasHistoryAsync(Guid id, CancellationToken cancellationToken);
    Task<BookItemDetailResponse> CreateAsync(BookItemWriteRequest request, CancellationToken cancellationToken);
    Task<BookItemDetailResponse?> UpdateAsync(Guid id, BookItemWriteRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
