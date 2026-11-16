using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

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
    public List<Reservation> Reservations { get; } = new();
}
