using Application.Commands;
using Application.Services;
using Microsoft.EntityFrameworkCore;

namespace Application.Test;

public class InventoryServiceTests : ServiceTestBase
{
    // ---- Reading --------------------------------------------------------------------------

    [Fact]
    public async Task GetItems_ReturnsEveryItemWithItsCategory()
    {
        var items = await InventoryService.GetItemsAsync();

        Assert.Equal(4, items.Count);
        var laptop = Assert.Single(items, item => item.Id == Data.LaptopId);
        Assert.Equal("SN-LAPTOP", laptop.SerialNumber);
        Assert.Equal("Developer Laptop", laptop.ItemName);
        Assert.Equal("Notebooks", laptop.Category);
        Assert.True(laptop.IsAvailable);
        Assert.False(laptop.IsOnLoan);
    }

    [Fact]
    public async Task GetItems_ItemOnLoan_ShowsWhoHasItAndUntilWhen()
    {
        var items = await InventoryService.GetItemsAsync();

        var cable = Assert.Single(items, item => item.Id == Data.CableId);
        Assert.NotNull(cable.CurrentLoan);
        Assert.Equal("Alan Turing", cable.CurrentLoan.PersonName);
        Assert.Equal("5BAIF", cable.CurrentLoan.SchoolClass);
        Assert.Equal(Fixture.CableLoanDate, cable.CurrentLoan.LoanDate);
        Assert.Equal(Fixture.CableLoanDate.AddDays(MaxLoanDays), cable.CurrentLoan.DueDate);
        Assert.False(cable.CurrentLoan.IsOverdue);
    }

    [Fact]
    public async Task GetItems_LoanPastItsDueDate_IsFlaggedOverdue()
    {
        var items = await InventoryService.GetItemsAsync();

        var dock = Assert.Single(items, item => item.Id == Data.DockId);
        Assert.True(dock.CurrentLoan!.IsOverdue);
    }

    [Fact]
    public async Task GetItems_ClosedLoan_DoesNotCountAsCurrentLoan()
    {
        var items = await InventoryService.GetItemsAsync();

        Assert.Null(Assert.Single(items, item => item.Id == Data.LaptopId).CurrentLoan);
    }

    [Fact]
    public async Task GetItem_ReturnsItemWithItsLoanHistory()
    {
        var details = await InventoryService.GetItemAsync(Data.LaptopId);

        Assert.NotNull(details);
        Assert.Equal("Developer Laptop", details.Item.ItemName);
        var entry = Assert.Single(details.History);
        Assert.Equal("Ada Lovelace", entry.PersonName);
        Assert.Equal(Fixture.LaptopReturnDate, entry.ReturnDate);
        Assert.Equal("Grace Hopper", entry.AcceptedBy);
    }

    [Fact]
    public async Task GetItem_UnknownId_ReturnsNull()
        => Assert.Null(await InventoryService.GetItemAsync(9999));

    [Fact]
    public async Task GetCategories_ReturnsAllCategoriesSortedByName()
    {
        var categories = await InventoryService.GetCategoriesAsync();

        Assert.Equal(["Cables", "Notebooks", "Projectors"], categories.Select(category => category.Text));
    }

    // ---- Adding ---------------------------------------------------------------------------

    [Fact]
    public async Task AddInventoryItem_ValidCommand_PersistsItem()
    {
        var id = await InventoryService.AddInventoryItemAsync(
            new AddInventoryItemCmd("SN-NEW-001", "Second Laptop", Data.NotebooksCategoryId, IsAvailable: true));

        using var db = CreateContext();
        var persisted = await db.InventoryItems.Include(item => item.Category).SingleAsync(item => item.Id == id);
        Assert.Equal("SN-NEW-001", persisted.SerialNumber);
        Assert.Equal("Second Laptop", persisted.ItemName);
        Assert.Equal(Data.NotebooksCategoryId, persisted.Category.Id);
        Assert.True(persisted.IsAvailable);
    }

    [Fact]
    public async Task AddInventoryItem_NormalizesSerialNumberAndName()
    {
        var id = await InventoryService.AddInventoryItemAsync(
            new AddInventoryItemCmd("  sn-new-002 ", "  Second Laptop  ", Data.NotebooksCategoryId, IsAvailable: true));

        using var db = CreateContext();
        var persisted = await db.InventoryItems.SingleAsync(item => item.Id == id);
        Assert.Equal("SN-NEW-002", persisted.SerialNumber);
        Assert.Equal("Second Laptop", persisted.ItemName);
    }

    [Fact]
    public async Task AddInventoryItem_UnknownCategory_IsRejectedAndNothingIsStored()
    {
        var exception = await Assert.ThrowsAsync<InventoryServiceException>(() =>
            InventoryService.AddInventoryItemAsync(
                new AddInventoryItemCmd("SN-NEW-003", "Orphan", CategoryId: 9999, IsAvailable: true)));

        Assert.Contains("Category", exception.Message);
        using var db = CreateContext();
        Assert.Equal(4, await db.InventoryItems.CountAsync());
    }

    [Theory]
    [InlineData("SN-LAPTOP")]
    [InlineData("sn-laptop")]
    [InlineData(" Sn-Laptop ")]
    public async Task AddInventoryItem_SerialNumberAlreadyInUse_IsRejected(string serialNumber)
    {
        var exception = await Assert.ThrowsAsync<InventoryServiceException>(() =>
            InventoryService.AddInventoryItemAsync(
                new AddInventoryItemCmd(serialNumber, "Duplicate", Data.NotebooksCategoryId, IsAvailable: true)));

        Assert.Contains("already in use", exception.Message);
    }

    [Fact]
    public async Task AddInventoryItem_SameSerialNumberSavedByAnotherUserFirst_IsRejected()
    {
        // The service finds the serial number free, then the other user commits it.
        BeforeNextSave(db => db.Add(new BaseModel.Model.InventoryItem(
            "SN-RACE", "Their Item", db.Categories.Single(c => c.Id == Data.NotebooksCategoryId), isAvailable: true)));

        var exception = await Assert.ThrowsAsync<InventoryServiceException>(() =>
            InventoryService.AddInventoryItemAsync(
                new AddInventoryItemCmd("SN-RACE", "My Item", Data.NotebooksCategoryId, IsAvailable: true)));

        Assert.Contains("already in use", exception.Message);
        using var verify = CreateContext();
        Assert.Equal("Their Item", (await verify.InventoryItems.SingleAsync(item => item.SerialNumber == "SN-RACE")).ItemName);
    }

    [Theory]
    [InlineData("", "Name", "Serial number is required")]
    [InlineData("   ", "Name", "Serial number is required")]
    [InlineData("SN-1", "", "Item name is required")]
    [InlineData("SN-1", "   ", "Item name is required")]
    public async Task AddInventoryItem_MissingText_IsRejected(string serialNumber, string itemName, string expectedMessage)
    {
        var exception = await Assert.ThrowsAsync<InventoryServiceException>(() =>
            InventoryService.AddInventoryItemAsync(
                new AddInventoryItemCmd(serialNumber, itemName, Data.NotebooksCategoryId, IsAvailable: true)));

        Assert.Contains(expectedMessage, exception.Message);
    }

    [Fact]
    public async Task AddInventoryItem_TextLongerThan255Characters_IsRejected()
    {
        var tooLong = new string('x', 256);

        var exception = await Assert.ThrowsAsync<InventoryServiceException>(() =>
            InventoryService.AddInventoryItemAsync(
                new AddInventoryItemCmd("SN-LONG", tooLong, Data.NotebooksCategoryId, IsAvailable: true)));

        Assert.Contains("255", exception.Message);
    }

    [Fact]
    public async Task AddInventoryItem_TextOfExactly255Characters_IsAccepted()
    {
        var id = await InventoryService.AddInventoryItemAsync(
            new AddInventoryItemCmd("SN-MAX", new string('x', 255), Data.NotebooksCategoryId, IsAvailable: true));

        Assert.NotEqual(0, id);
    }

    // ---- Availability ---------------------------------------------------------------------

    [Fact]
    public async Task UpdateAvailability_BlocksAnAvailableItem()
    {
        await InventoryService.UpdateAvailabilityAsync(new UpdateAvailabilityCmd(Data.LaptopId, IsAvailable: false));

        using var db = CreateContext();
        Assert.False((await db.InventoryItems.SingleAsync(item => item.Id == Data.LaptopId)).IsAvailable);
    }

    [Fact]
    public async Task UpdateAvailability_ReleasesABlockedItem()
    {
        await InventoryService.UpdateAvailabilityAsync(new UpdateAvailabilityCmd(Data.BeamerId, IsAvailable: true));

        using var db = CreateContext();
        Assert.True((await db.InventoryItems.SingleAsync(item => item.Id == Data.BeamerId)).IsAvailable);
    }

    [Fact]
    public async Task UpdateAvailability_ItemOnLoan_CannotBeReleased()
    {
        var exception = await Assert.ThrowsAsync<InventoryServiceException>(() =>
            InventoryService.UpdateAvailabilityAsync(new UpdateAvailabilityCmd(Data.CableId, IsAvailable: true)));

        Assert.Contains("on loan", exception.Message);
        using var db = CreateContext();
        Assert.False((await db.InventoryItems.SingleAsync(item => item.Id == Data.CableId)).IsAvailable);
    }

    [Fact]
    public async Task UpdateAvailability_UnknownId_ThrowsInventoryNotFoundException()
        => await Assert.ThrowsAsync<InventoryNotFoundException>(() =>
            InventoryService.UpdateAvailabilityAsync(new UpdateAvailabilityCmd(Id: 9999, IsAvailable: true)));
}
