using Application.Commands;
using Application.Services;
using Microsoft.EntityFrameworkCore;

namespace Application.Test;

/// <summary>
/// Tests for <see cref="InventoryService.UpdateAvailability"/> covering a successful toggle and
/// the <see cref="InventoryNotFoundException"/> raised for a non-existing item.
/// </summary>
public class UpdateAvailabilityTests : InventoryServiceTestBase
{
    [Fact]
    public async Task UpdateAvailability_TogglesFlagToFalse_Persists()
    {
        // Arrange
        using var actContext = CreateContext();
        var service = CreateService(actContext);

        // Act
        await service.UpdateAvailability(new UpdateAvailabilityCmd(AvailableItemId, IsAvailable: false));

        // Assert
        using var assertContext = CreateContext();
        var item = await assertContext.InventoryItems.SingleAsync(i => i.Id == AvailableItemId);
        Assert.False(item.IsAvailable);
    }

    [Fact]
    public async Task UpdateAvailability_TogglesFlagToTrue_Persists()
    {
        // Arrange
        using var actContext = CreateContext();
        var service = CreateService(actContext);

        // Act
        await service.UpdateAvailability(new UpdateAvailabilityCmd(UnavailableItemId, IsAvailable: true));

        // Assert
        using var assertContext = CreateContext();
        var item = await assertContext.InventoryItems.SingleAsync(i => i.Id == UnavailableItemId);
        Assert.True(item.IsAvailable);
    }

    [Fact]
    public async Task UpdateAvailability_WithUnknownId_ThrowsInventoryNotFoundException()
    {
        // Arrange
        using var actContext = CreateContext();
        var service = CreateService(actContext);

        // Act & Assert
        await Assert.ThrowsAsync<InventoryNotFoundException>(
            () => service.UpdateAvailability(new UpdateAvailabilityCmd(Id: 9999, IsAvailable: true)));
    }
}
