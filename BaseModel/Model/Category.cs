using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

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
    [JsonIgnore]
    public List<InventoryItem> IntentoryItems { get; } = new();
}
