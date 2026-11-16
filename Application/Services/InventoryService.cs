using Application.Commands;
using Application.ReadModels;
using BaseModel.Infrastructure;
using BaseModel.Model;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Services;

/// <summary>
/// Manages the inventory itself: which items exist and whether they may be lent.
/// Every operation works on its own short-lived <see cref="ReservationContext"/>. A Blazor Server
/// circuit lives for minutes or hours, so one context per circuit would serve stale data and
/// break as soon as two UI events overlap.
/// </summary>
public class InventoryService(IDbContextFactory<ReservationContext> dbFactory, LoanPolicy loanPolicy)
{
    private const int MaxTextLength = 255;

    public async Task<IReadOnlyList<InventoryItemSummary>> GetItemsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.InventoryItems
            .OrderBy(item => item.Id)
            .ToSummariesAsync(loanPolicy, cancellationToken);
    }

    /// <summary>Returns an item with its loan history, or null when the id is unknown.</summary>
    public async Task<InventoryItemDetails?> GetItemAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var summaries = await db.InventoryItems
            .Where(item => item.Id == id)
            .ToSummariesAsync(loanPolicy, cancellationToken);
        if (summaries.Count == 0)
            return null;

        var history = await db.Reservations.ToHistoryAsync(id, cancellationToken);
        return new InventoryItemDetails(summaries[0], history);
    }

    public async Task<IReadOnlyList<LookupEntry>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Categories
            .AsNoTracking()
            .OrderBy(category => category.Text)
            .Select(category => new LookupEntry(category.Id, category.Text))
            .ToListAsync(cancellationToken);
    }

    /// <returns>The id of the new item.</returns>
    public async Task<int> AddInventoryItemAsync(AddInventoryItemCmd cmd, CancellationToken cancellationToken = default)
    {
        // Serial numbers are compared case-insensitively: "nb-1" and "NB-1" are the same device.
        var serialNumber = RequireText(cmd.SerialNumber, "Serial number").ToUpperInvariant();
        var itemName = RequireText(cmd.ItemName, "Item name");

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == cmd.CategoryId, cancellationToken)
            ?? throw new InventoryServiceException("Category not found.");

        if (await db.InventoryItems.AnyAsync(item => item.SerialNumber == serialNumber, cancellationToken))
            throw SerialNumberInUse(serialNumber);

        var inventoryItem = new InventoryItem(serialNumber, itemName, category, cmd.IsAvailable);
        db.Add(inventoryItem);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException e) when (e.IsUniqueConstraintViolation())
        {
            // Someone else saved the same serial number between our check and our insert.
            throw SerialNumberInUse(serialNumber, e);
        }

        return inventoryItem.Id;
    }

    public async Task UpdateAvailabilityAsync(UpdateAvailabilityCmd cmd, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var inventoryItem = await db.InventoryItems.FirstOrDefaultAsync(item => item.Id == cmd.Id, cancellationToken)
            ?? throw new InventoryNotFoundException("Inventory item not found.");

        var isOnLoan = await db.Reservations.AnyAsync(
            reservation => reservation.InventoryItem.Id == cmd.Id && reservation.ReturnDate == null,
            cancellationToken);
        if (cmd.IsAvailable && isOnLoan)
            throw new InventoryServiceException($"'{inventoryItem.ItemName}' is on loan. Take it back before releasing it.");

        inventoryItem.IsAvailable = cmd.IsAvailable;
        await db.SaveChangesAsync(cancellationToken);
    }

    // The service checks its own input: the form validates too, but a caller other than the
    // form (a test, an import, a future API) must not be able to store an unusable item.
    private static string RequireText(string? value, string fieldName)
    {
        var text = value?.Trim() ?? string.Empty;
        if (text.Length == 0)
            throw new InventoryServiceException($"{fieldName} is required.");
        if (text.Length > MaxTextLength)
            throw new InventoryServiceException($"{fieldName} must not be longer than {MaxTextLength} characters.");
        return text;
    }

    private static InventoryServiceException SerialNumberInUse(string serialNumber, DbUpdateException? cause = null)
        => new($"Serial number '{serialNumber}' is already in use.", cause);
}
