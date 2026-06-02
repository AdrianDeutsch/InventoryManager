using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace BaseModel.Model;

public class Status
{
    [SetsRequiredMembers]
    public Status(string text)
    {
        Text = text;
    }

    public int Id { get; set; }
    public required string Text { get; set; }
    [JsonIgnore]
    public List<Reservation> Reservations { get; } = new();
}
