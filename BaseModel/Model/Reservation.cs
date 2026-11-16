using System;
using System.Diagnostics.CodeAnalysis;

namespace BaseModel.Model;

/// <summary>A loan of one inventory item to one person.</summary>
public class Reservation
{
    protected Reservation() { }

    /// <summary>Creates an open loan: the item is handed out on <paramref name="loanDate"/>.</summary>
    [SetsRequiredMembers]
    public Reservation(Person person, InventoryItem inventoryItem, DateOnly loanDate)
    {
        Person = person;
        InventoryItem = inventoryItem;
        LoanDate = loanDate;
        Status = ReservationStatus.Issued;
    }

    public int Id { get; set; }
    public required Person Person { get; set; }
    public required InventoryItem InventoryItem { get; set; }
    public required DateOnly LoanDate { get; set; }
    public ReservationStatus Status { get; private set; }
    public DateOnly? ReturnDate { get; private set; }

    /// <summary>The staff member who took the item back; only set once the loan is closed.</summary>
    public Staff? AcceptedBy { get; private set; }

    public bool IsOpen => ReturnDate is null;

    /// <summary>
    /// Closes the loan. Status, return date and accepting staff always change together, so
    /// this is the only way to set them.
    /// </summary>
    public void Return(DateOnly returnDate, Staff acceptedBy, bool damaged)
    {
        if (!IsOpen)
            throw new InvalidOperationException($"Reservation {Id} has already been returned.");

        ReturnDate = returnDate;
        AcceptedBy = acceptedBy;
        Status = damaged ? ReservationStatus.DamagedReturn : ReservationStatus.Returned;
    }
}
