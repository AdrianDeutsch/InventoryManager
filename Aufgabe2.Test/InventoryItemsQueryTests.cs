using BaseModel.Model;
using Microsoft.EntityFrameworkCore;

namespace Aufgabe2.Test;

/// <summary>
/// Tests for the read-only <c>InventoryService.InventoryItems</c> query surface that the
/// Blazor overview consumes, including eager loading of reservations.
/// </summary>
public class InventoryItemsQueryTests : InventoryServiceTestBase
{
    [Fact]
    public async Task InventoryItems_ReturnsAllSeededItems()
    {
        // Arrange
        using var context = CreateContext();
        var service = CreateService(context);

        // Act
        var items = await service.InventoryItems.ToListAsync();

        // Assert
        Assert.Equal(2, items.Count);
        Assert.Contains(items, item => item.Id == AvailableItemId);
        Assert.Contains(items, item => item.Id == UnavailableItemId);
    }

    [Fact]
    public async Task InventoryItems_CanEagerLoadReservationsWithPerson()
    {
        // Arrange
        using var context = CreateContext();
        var service = CreateService(context);

        // Act
        var item = await service.InventoryItems
            .Include(i => i.Reservations).ThenInclude(reservation => reservation.Person)
            .SingleAsync(i => i.Id == AvailableItemId);

        // Assert
        var reservation = Assert.Single(item.Reservations);
        Assert.Equal("Ada", reservation.Person.FirstName);
        Assert.Equal("Lovelace", reservation.Person.LastName);
    }

    [Fact]
    public async Task InventoryItems_ItemWithoutReservations_HasEmptyReservationCollection()
    {
        // Arrange
        using var context = CreateContext();
        var service = CreateService(context);

        // Act
        var item = await service.InventoryItems
            .Include(i => i.Reservations)
            .SingleAsync(i => i.Id == UnavailableItemId);

        // Assert
        Assert.Empty(item.Reservations);
    }
}
