using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace BaseModel.Model;

public class Staff
{
    protected Staff() { }
    [SetsRequiredMembers]
    public Staff(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
    }

    public int Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    [JsonIgnore]
    public List<Reservation> Reservations { get; } = new();
}
