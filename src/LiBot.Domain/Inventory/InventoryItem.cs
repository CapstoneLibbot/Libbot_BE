namespace LiBot.Domain.Inventory;

public class InventoryItem
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; }
    public int Quantity { get; private set; }
    public string Unit { get; private set; } = "pcs";

    public InventoryItem(string name, int quantity, string unit = "pcs")
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Item name is required.", nameof(name));

        if (quantity < 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        Name = name.Trim();
        Quantity = quantity;
        Unit = unit.Trim();
    }

    public void AddQuantity(int amount)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount));

        Quantity += amount;
    }

    public void RemoveQuantity(int amount)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount));

        if (Quantity - amount < 0)
            throw new InvalidOperationException("Not enough quantity in stock.");

        Quantity -= amount;
    }
}
