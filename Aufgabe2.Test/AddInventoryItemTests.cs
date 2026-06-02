using Aufgabe2.Commands;
using Aufgabe2.Services;
using BaseModel.Model;
using Microsoft.EntityFrameworkCore;

namespace Aufgabe2.Test;

/// <summary>
/// Tests for <see cref="InventoryService.AddInventoryItem"/> covering the success path and the
/// domain failure raised when the referenced category does not exist.
/// </summary>
public class AddInventoryItemTests : InventoryServiceTestBase
{
    [Fact]
    public async Task AddInventoryItem_WithValidCommand_PersistsItemWithGeneratedId()
    {
        // Arrange
        using var actContext = CreateContext();
        var service = CreateService(actContext);
        var command = new AddInventoryItemCmd("SN-NEW-001", "Docking Station", NotebooksCategoryId, IsAvailable: true);

        // Act
        var created = await service.AddInventoryItem(command);

        // Assert
        Assert.NotEqual(0, created.Id);

        using var assertContext = CreateContext();
        var persisted = await assertContext.InventoryItems
            .Include(item => item.Category)
            .SingleAsync(item => item.Id == created.Id);

        Assert.Equal("SN-NEW-001", persisted.SerialNumber);
        Assert.Equal("Docking Station", persisted.ItemName);
        Assert.True(persisted.IsAvailable);
        Assert.Equal(NotebooksCategoryId, persisted.Category.Id);
    }

    [Fact]
    public async Task AddInventoryItem_WithUnknownCategory_ThrowsInventoryServiceException()
    {
        // Arrange
        using var actContext = CreateContext();
        var service = CreateService(actContext);
        var command = new AddInventoryItemCmd("SN-NEW-002", "Orphan Item", CategoryId: 9999, IsAvailable: true);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InventoryServiceException>(
            () => service.AddInventoryItem(command));
        Assert.Contains("category", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AddInventoryItem_WithUnknownCategory_DoesNotPersistAnything()
    {
        // Arrange
        using var actContext = CreateContext();
        var service = CreateService(actContext);
        var countBefore = await actContext.InventoryItems.CountAsync();
        var command = new AddInventoryItemCmd("SN-NEW-003", "Orphan Item", CategoryId: 9999, IsAvailable: true);

        // Act
        await Assert.ThrowsAsync<InventoryServiceException>(() => service.AddInventoryItem(command));

        // Assert
        using var assertContext = CreateContext();
        Assert.Equal(countBefore, await assertContext.InventoryItems.CountAsync());
    }
}
