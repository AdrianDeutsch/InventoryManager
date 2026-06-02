using BaseModel.Infrastructure;
using BaseModel.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Aufgabe1;

public record CategoryWithCount(string Text, int Count);

public class ReservationService
{
    private readonly ReservationContext _db;
    public ReservationService(ReservationContext db)
    {
        _db = db;
    }

    public List<CategoryWithCount> GetCategoriesWithCount()
    {
        throw new NotImplementedException();
    }

    public List<Person> GetPersonsWithOpenReservations()
    {
        throw new NotImplementedException();
    }

    public List<InventoryItem> GetInventoryItemsWithoutReservations(int year)
    {
        throw new NotImplementedException();
    }

    public Reservation AddReservation(int personId, int inventoryItemId, DateOnly loanDate)
    {
        throw new NotImplementedException();
    }
}
