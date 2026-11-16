using Application.Commands;
using Application.ReadModels;
using BaseModel.Infrastructure;
using BaseModel.Model;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Services;

/// <summary>
/// The lending workflow: hand an item out, take it back, and the reports built on the loans.
/// </summary>
public class ReservationService(IDbContextFactory<ReservationContext> dbFactory, LoanPolicy loanPolicy)
{
    // ---- Commands -------------------------------------------------------------------------

    /// <summary>Hands an item out. The item is blocked for others until it is returned.</summary>
    /// <returns>The id of the new loan.</returns>
    public async Task<int> LendAsync(LendItemCmd cmd, CancellationToken cancellationToken = default)
    {
        if (cmd.LoanDate > loanPolicy.Today)
            throw new ReservationException("The loan date cannot be in the future.");

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var inventoryItem = await db.InventoryItems.FirstOrDefaultAsync(item => item.Id == cmd.InventoryItemId, cancellationToken)
            ?? throw new ReservationException("Inventory item not found.");
        var person = await db.Persons.FirstOrDefaultAsync(p => p.Id == cmd.PersonId, cancellationToken)
            ?? throw new ReservationException("Person not found.");

        var loansOfItem = db.Reservations.Where(reservation => reservation.InventoryItem.Id == inventoryItem.Id);
        if (await loansOfItem.AnyAsync(reservation => reservation.ReturnDate == null, cancellationToken))
            throw new ReservationException($"'{inventoryItem.ItemName}' is already on loan.");
        if (!inventoryItem.IsAvailable)
            throw new ReservationException($"'{inventoryItem.ItemName}' is blocked and cannot be lent.");

        // Loans may be recorded after the fact, but the history of an item must not overlap.
        var lastReturn = await loansOfItem.MaxAsync(reservation => reservation.ReturnDate, cancellationToken);
        if (lastReturn is not null && cmd.LoanDate < lastReturn)
            throw new ReservationException(
                $"'{inventoryItem.ItemName}' was only returned on {lastReturn:yyyy-MM-dd}. The loan cannot start before that.");

        var reservation = new Reservation(person, inventoryItem, cmd.LoanDate);
        inventoryItem.IsAvailable = false;
        db.Add(reservation);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException e) when (e.IsUniqueConstraintViolation())
        {
            // Two users lent the same item at the same moment. Both passed the checks above,
            // but the open-loan index of the database let only the first one through.
            throw new ReservationException($"'{inventoryItem.ItemName}' was lent by someone else in the meantime.", e);
        }

        return reservation.Id;
    }

    /// <summary>
    /// Takes an item back. An item in working condition is available again at once; a damaged
    /// one stays blocked until staff releases it.
    /// </summary>
    public async Task ReturnAsync(ReturnItemCmd cmd, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var reservation = await db.Reservations
            .Include(r => r.InventoryItem)
            .FirstOrDefaultAsync(r => r.Id == cmd.ReservationId, cancellationToken)
            ?? throw new ReservationException("Loan not found.");

        if (!reservation.IsOpen)
            throw AlreadyReturned();
        if (cmd.ReturnDate < reservation.LoanDate)
            throw new ReservationException(
                $"The return date cannot be before the loan date ({reservation.LoanDate:yyyy-MM-dd}).");
        if (cmd.ReturnDate > loanPolicy.Today)
            throw new ReservationException("The return date cannot be in the future.");

        var staff = await db.Staffs.FirstOrDefaultAsync(s => s.Id == cmd.StaffId, cancellationToken)
            ?? throw new ReservationException("Staff member not found.");

        reservation.Return(cmd.ReturnDate, staff, cmd.Damaged);
        reservation.InventoryItem.IsAvailable = !cmd.Damaged;
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException e)
        {
            // The return date is a concurrency token: someone else closed this loan first.
            throw AlreadyReturned(e);
        }
    }

    // ---- Queries --------------------------------------------------------------------------

    /// <summary>All loans that are still out, the oldest first.</summary>
    public async Task<IReadOnlyList<OpenLoan>> GetOpenLoansAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var rows = await db.Reservations
            .AsNoTracking()
            .Where(reservation => reservation.ReturnDate == null)
            .OrderBy(reservation => reservation.LoanDate)
            .Select(reservation => new
            {
                reservation.Id,
                ItemId = reservation.InventoryItem.Id,
                reservation.InventoryItem.ItemName,
                reservation.InventoryItem.SerialNumber,
                reservation.Person.FirstName,
                reservation.Person.LastName,
                reservation.Person.SchoolClass,
                reservation.LoanDate,
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new OpenLoan(
                row.Id, row.ItemId, row.ItemName, row.SerialNumber,
                $"{row.FirstName} {row.LastName}", row.SchoolClass, row.LoanDate,
                loanPolicy.DueDate(row.LoanDate), loanPolicy.IsOverdue(row.LoanDate)))
            .ToList();
    }

    /// <summary>Loan history of one item, newest loan first.</summary>
    public async Task<IReadOnlyList<LoanHistoryEntry>> GetHistoryAsync(
        int inventoryItemId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Reservations.ToHistoryAsync(inventoryItemId, cancellationToken);
    }

    /// <summary>Every category with the number of items in it, including empty categories.</summary>
    public async Task<IReadOnlyList<CategoryWithCount>> GetCategoriesWithCountAsync(
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Categories
            .AsNoTracking()
            .OrderBy(category => category.Text)
            .Select(category => new CategoryWithCount(category.Text, category.InventoryItems.Count))
            .ToListAsync(cancellationToken);
    }

    /// <summary>Everyone who currently holds at least one item.</summary>
    public async Task<IReadOnlyList<Borrower>> GetPersonsWithOpenReservationsAsync(
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Persons
            .AsNoTracking()
            .Where(person => person.Reservations.Any(reservation => reservation.ReturnDate == null))
            .OrderBy(person => person.LastName).ThenBy(person => person.FirstName)
            .Select(person => new Borrower(
                person.Id,
                person.FirstName + " " + person.LastName,
                person.SchoolClass,
                person.Email,
                person.Reservations.Count(reservation => reservation.ReturnDate == null)))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Items that were not handed out in the given calendar year. A loan counts for the year it
    /// started in.
    /// </summary>
    public async Task<IReadOnlyList<InventoryItemSummary>> GetInventoryItemsWithoutReservationsAsync(
        int year, CancellationToken cancellationToken = default)
    {
        if (year < 1 || year >= DateOnly.MaxValue.Year)
            throw new ReservationException($"{year} is not a valid year.");

        var firstDay = new DateOnly(year, 1, 1);
        var firstDayOfNextYear = firstDay.AddYears(1);

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.InventoryItems
            .Where(item => !item.Reservations.Any(
                reservation => reservation.LoanDate >= firstDay && reservation.LoanDate < firstDayOfNextYear))
            .OrderBy(item => item.ItemName)
            .ToSummariesAsync(loanPolicy, cancellationToken);
    }

    public async Task<IReadOnlyList<LookupEntry>> GetPersonsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Persons
            .AsNoTracking()
            .OrderBy(person => person.LastName).ThenBy(person => person.FirstName)
            .Select(person => new LookupEntry(
                person.Id, person.LastName + " " + person.FirstName + " (" + person.SchoolClass + ")"))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LookupEntry>> GetStaffAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Staffs
            .AsNoTracking()
            .OrderBy(staff => staff.LastName).ThenBy(staff => staff.FirstName)
            .Select(staff => new LookupEntry(staff.Id, staff.LastName + " " + staff.FirstName))
            .ToListAsync(cancellationToken);
    }

    private static ReservationException AlreadyReturned(Exception? cause = null)
        => new("This loan has already been returned.", cause);
}
