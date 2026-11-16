using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace BaseModel.Model;

public class InventoryItem
{
    protected InventoryItem() { }
    [SetsRequiredMembers]
    public InventoryItem(string serialNumber, string itemName, Category category, bool isAvailable)
    {
        SerialNumber = serialNumber;
        ItemName = itemName;
        Category = category;
        IsAvailable = isAvailable;
    }

    public int Id { get; set; }
    public required string SerialNumber { get; set; }
    public required string ItemName { get; set; }
    public required Category Category { get; set; }
    public required bool IsAvailable { get; set; }
    public List<Reservation> Reservations { get; } = new();

}
