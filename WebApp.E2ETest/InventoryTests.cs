using WebApp.E2ETest.Pages;
using static Microsoft.Playwright.Assertions;

namespace WebApp.E2ETest;

public class InventoryTests(AppFixture app) : E2ETestBase(app)
{
    [Fact]
    public Task Overview_ShowsTheSeededInventory() => RunAsync(async page =>
    {
        var list = new InventoryListPage(page);

        await list.OpenAsync();

        await Expect(page).ToHaveTitleAsync("Inventory");
        // The demo data has 14 items; other tests may have added more. The grid shows 10 per page.
        Assert.True(await InventoryListPage.NumberAsync(list.TotalItems) >= 14);
        await Expect(list.Rows).ToHaveCountAsync(10);
    });

    [Fact]
    public Task Search_NarrowsTheListDown() => RunAsync(async page =>
    {
        var list = new InventoryListPage(page);
        await list.OpenAsync();

        await list.SearchAsync("projector");

        await Expect(list.Rows).ToHaveCountAsync(2);
        await Expect(list.Rows.First).ToContainTextAsync("projector");

        await list.SearchAsync("no such item anywhere");

        await Expect(list.Rows).ToHaveCountAsync(0);
        await Expect(page.GetByText("No inventory items match your search.")).ToBeVisibleAsync();
    });

    [Fact]
    public Task ExpandingARow_ShowsTheLoanHistoryOfThatItem() => RunAsync(async page =>
    {
        var list = new InventoryListPage(page);
        await list.OpenAsync();
        // The first demo notebook has an open loan, so its history is never empty.
        await list.SearchAsync("ThinkPad");

        await list.Row("ThinkPad").Locator("td.rz-col-icon a, td.rz-col-icon button").First.ClickAsync();

        var history = page.Locator("[data-test='loan-history'] tbody tr.rz-data-row");
        await Expect(history.First).ToBeVisibleAsync();
        await Expect(history.First).ToContainTextAsync("On loan");
    });

    [Fact]
    public Task ExpandedRow_KeepsItsHistoryWhenAnotherRowChanges() => RunAsync(async page =>
    {
        // An item of our own next to the demo ThinkPad, so both rows match one search.
        var serialNumber = UniqueSerialNumber();
        var ownItem = $"ThinkPad sleeve {serialNumber}";
        await new CreateItemPage(page).CreateAsync(serialNumber, ownItem, category: "Cables");
        var list = new InventoryListPage(page);
        await list.OpenAsync();
        await list.SearchAsync("ThinkPad");
        await list.Row("Lenovo ThinkPad").Locator("td.rz-col-icon a, td.rz-col-icon button").First.ClickAsync();
        var history = page.Locator("[data-test='loan-history'] tbody tr.rz-data-row");
        await Expect(history.First).ToBeVisibleAsync();

        // Blocking the other item reloads the list.
        await list.ToggleLendableAsync(ownItem);
        await Expect(list.StatusOf(ownItem)).ToHaveTextAsync("Blocked");

        // The expanded row must still show its history, not a loading bar that never ends.
        await Expect(history.First).ToBeVisibleAsync();
        await Expect(history.First).ToContainTextAsync("On loan");
    });

    [Fact]
    public Task CreateItem_EmptyForm_ShowsValidationMessagesAndStaysOnTheForm() => RunAsync(async page =>
    {
        var form = new CreateItemPage(page);
        await form.OpenAsync();

        await form.SubmitAsync();

        await Expect(form.ValidationMessages).ToHaveTextAsync(
            ["Serial number is required.", "Item name is required.", "Please select a category."]);
        await Expect(page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/inventory/create$"));
    });

    [Fact]
    public Task CreateItem_StoresTheSerialNumberInUpperCase() => RunAsync(async page =>
    {
        var serialNumber = UniqueSerialNumber("e2e-lower");

        await new CreateItemPage(page).CreateAsync(serialNumber, "Lower Case Laptop");

        await Expect(new ItemDetailPage(page).SerialNumber).ToHaveTextAsync(serialNumber.ToUpperInvariant());
    });

    [Fact]
    public Task CreateItem_DuplicateSerialNumber_IsRejectedAndTheFormKeepsItsValues() => RunAsync(async page =>
    {
        var serialNumber = UniqueSerialNumber();
        var form = new CreateItemPage(page);
        await form.CreateAsync(serialNumber, "Original");

        await form.OpenAsync();
        await form.FillAsync(serialNumber.ToLowerInvariant(), "Copy", "Cables");
        await form.SubmitAsync();

        await Expect(page.Notification("already in use")).ToBeVisibleAsync();
        await Expect(page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/inventory/create$"));
        await Expect(page.Locator("[data-test='item-name']")).ToHaveValueAsync("Copy");
    });

    [Fact]
    public Task UnknownItem_ShowsANotFoundMessage() => RunAsync(async page =>
    {
        var detail = new ItemDetailPage(page);

        await detail.OpenAsync(999_999);

        await Expect(detail.NotFoundMessage).ToContainTextAsync("No inventory item with id 999999 exists.");
    });

    [Fact]
    public Task Reports_ShowCategoriesBorrowersAndUnusedItems() => RunAsync(async page =>
    {
        await page.GotoAsync("/reports");

        await Expect(page.Locator("[data-test='report-categories']")).ToContainTextAsync("Projectors");
        await Expect(page.Locator("[data-test='report-borrowers'] tbody tr.rz-data-row").First).ToBeVisibleAsync();
        await Expect(page.Locator("[data-test='report-unused'] tbody tr.rz-data-row").First).ToBeVisibleAsync();
    });

    [Fact]
    public async Task HealthEndpoint_ReportsHealthy()
    {
        using var http = new HttpClient();

        var body = await http.GetStringAsync($"{AppBaseUrl}/healthz");

        Assert.Equal("Healthy", body);
    }
}
