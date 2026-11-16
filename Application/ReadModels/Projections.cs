using Application.Services;
using BaseModel.Model;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.ReadModels;

/// <summary>Queries shared by both services, so every screen shows an item the same way.</summary>
internal static class Projections
{
    /// <summary>
    /// Loads the given items together with their open loan in one SQL statement. The due date
    /// and the overdue flag depend on the loan policy, so they are added after the query.
    /// </summary>
    public static async Task<List<InventoryItemSummary>> ToSummariesAsync(
        this IQueryable<InventoryItem> items, LoanPolicy loanPolicy, CancellationToken cancellationToken)
    {
        var rows = await items
            .AsNoTracking()
            .Select(item => new
            {
                item.Id,
                item.SerialNumber,
                item.ItemName,
                Category = item.Category.Text,
                item.IsAvailable,
                OpenLoan = item.Reservations
                    .Where(reservation => reservation.ReturnDate == null)
                    .Select(reservation => new
                    {
                        reservation.Id,
                        reservation.Person.FirstName,
                        reservation.Person.LastName,
                        reservation.Person.SchoolClass,
                        reservation.LoanDate,
                    })
                    .FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new InventoryItemSummary(
                row.Id, row.SerialNumber, row.ItemName, row.Category, row.IsAvailable,
                row.OpenLoan is null
                    ? null
                    : new CurrentLoan(
                        row.OpenLoan.Id,
                        $"{row.OpenLoan.FirstName} {row.OpenLoan.LastName}",
                        row.OpenLoan.SchoolClass,
                        row.OpenLoan.LoanDate,
                        loanPolicy.DueDate(row.OpenLoan.LoanDate),
                        loanPolicy.IsOverdue(row.OpenLoan.LoanDate))))
            .ToList();
    }

    /// <summary>Loan history of one item, newest loan first.</summary>
    public static Task<List<LoanHistoryEntry>> ToHistoryAsync(
        this IQueryable<Reservation> reservations, int inventoryItemId, CancellationToken cancellationToken)
        => reservations
            .AsNoTracking()
            .Where(reservation => reservation.InventoryItem.Id == inventoryItemId)
            .OrderByDescending(reservation => reservation.LoanDate)
            .Select(reservation => new LoanHistoryEntry(
                reservation.Id,
                reservation.Person.FirstName + " " + reservation.Person.LastName,
                reservation.Person.SchoolClass,
                reservation.LoanDate,
                reservation.ReturnDate,
                reservation.Status,
                reservation.AcceptedBy == null
                    ? null
                    : reservation.AcceptedBy.FirstName + " " + reservation.AcceptedBy.LastName))
            .ToListAsync(cancellationToken);
}
