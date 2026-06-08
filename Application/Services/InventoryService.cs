// =================================================================================================
// Vorgegebenes Service für die REST Schnittstelle.
// Hier ist nichts zu implementieren.
// =================================================================================================
using BaseModel.Infrastructure;
using BaseModel.Model;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Application.Commands;

namespace Application.Services;

public class InventoryService(ReservationContext db)
{
    public IQueryable<InventoryItem> InventoryItems => db.Set<InventoryItem>().AsQueryable();
    public async Task<InventoryItem> AddInventoryItem(AddInventoryItemCmd cmd)
    {
        var priceEntry = new InventoryItem(
            cmd.SerialNumber, cmd.ItemName,
            await FirstOrThrow<Category>(c => c.Id == cmd.CategoryId, "Category not found"),
            cmd.IsAvailable);
        db.Add(priceEntry);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException e)
        {
            throw new InventoryServiceException(e.InnerException?.Message ?? e.Message);
        }
        return priceEntry;
    }
    public async Task UpdateAvailability(UpdateAvailabilityCmd cmd)
    {
        var inventoryItem = await FindOrThrow<InventoryItem>(
            s => s.Id == cmd.Id, "Inventory item not found.");
        inventoryItem.IsAvailable = cmd.IsAvailable;
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException e)
        {
            throw new InventoryServiceException(e.InnerException?.Message ?? e.Message);
        }
    }

    private async Task<TEntity> FirstOrThrow<TEntity>(Expression<Func<TEntity, bool>> predicate, string message) where TEntity : class
        => await db.Set<TEntity>().FirstOrDefaultAsync(predicate) ?? throw new InventoryServiceException(message);
    private async Task<TEntity> FindOrThrow<TEntity>(Expression<Func<TEntity, bool>> predicate, string message) where TEntity : class
        => await db.Set<TEntity>().FirstOrDefaultAsync(predicate) ?? throw new InventoryNotFoundException(message);
}