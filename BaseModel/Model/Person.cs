using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace BaseModel.Model;

public class Person
{
    protected Person() { }
    [SetsRequiredMembers]
    public Person(string firstName, string lastName, string schoolClass, string email, string phone)
    {
        FirstName = firstName;
        LastName = lastName;
        SchoolClass = schoolClass;
        Email = email;
        Phone = phone;
    }

    public int Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string SchoolClass { get; set; }
    public required string Email { get; set; }
    public required string Phone { get; set; }
    [JsonIgnore]
    public List<Reservation> Reservations { get; } = new();
}
