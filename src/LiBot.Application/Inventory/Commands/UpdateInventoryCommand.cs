namespace LiBot.Application.Inventory.Commands;

public record UpdateInventoryCommand(string ItemName, int QuantityDelta);
