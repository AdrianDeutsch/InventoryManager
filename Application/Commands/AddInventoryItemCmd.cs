namespace Application.Commands;

/// <summary>Adds a new item to the inventory. Serial numbers are unique across the inventory.</summary>
public record AddInventoryItemCmd(
    string SerialNumber,
    string ItemName,
    int CategoryId,
    bool IsAvailable);
