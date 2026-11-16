using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace BaseModel.Model;

public class Category
{
    protected Category() { }
    [SetsRequiredMembers]
    public Category(string text)
    {
        Text = text;
    }
    public int Id { get; set; }
    public required string Text { get; set; }
    public List<InventoryItem> InventoryItems { get; } = new();
}
