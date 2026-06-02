using System;
using System.Diagnostics.CodeAnalysis;

namespace BaseModel.Model;
public class Reservation
{
    protected Reservation() { }
    [SetsRequiredMembers]
    public Reservation(
        Person person, InventoryItem inventoryItem, DateOnly loanDate,
        Status status, DateOnly? returnDate = null, Staff? acceptedBy = null)
    {
        Person = person;
        InventoryItem = inventoryItem;
        LoanDate = loanDate;
        Status = status;
        ReturnDate = returnDate;
        AcceptedBy = acceptedBy;
    }
    public int Id { get; set; }
    public required Person Person { get; set; }
    public required InventoryItem InventoryItem { get; set; }
    public required DateOnly LoanDate { get; set; }
    public required Status Status { get; set; }
    public DateOnly? ReturnDate { get; set; }
    public Staff? AcceptedBy { get; set; }
}
