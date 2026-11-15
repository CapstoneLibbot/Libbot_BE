namespace LiBot.Contracts.Library;

// Public wire contracts for new Library endpoints. Existing Application models remain adapters
// until legacy endpoints are migrated without changing their JSON representation.
public sealed record FloorOption(Guid Id, string Name, int FloorNumber, string Status);
public sealed record AisleOption(Guid Id, Guid FloorId, string Name, string Status);
public sealed record ShelfOption(Guid Id, Guid AisleId, string Name, string Status);
public sealed record BookItemWriteContract(Guid BookId, string Barcode, Guid? ExpectedShelfLevelId, Guid? CurrentShelfLevelId, string Status);
public sealed record BookItemLocationContract(Guid FloorId, string FloorName, int FloorNumber, Guid AisleId, string AisleName, Guid ShelfId, string ShelfName, Guid ShelfLevelId, int LevelNumber);
