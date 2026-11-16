namespace WebApp.E2ETest.Pages;

/// <summary>The inventory overview ("/").</summary>
public sealed class InventoryListPage(IPage page)
{
    private ILocator Grid => page.Locator("[data-test='inventory-grid']");

    public ILocator Rows => Grid.Locator("tbody tr.rz-data-row");

    public ILocator TotalItems => page.Locator("[data-test='stat-total']");
    public ILocator Available => page.Locator("[data-test='stat-available']");
    public ILocator OnLoan => page.Locator("[data-test='stat-on-loan']");

    public async Task OpenAsync()
    {
        await page.GotoAsync("/");
        await Rows.First.WaitForAsync();
    }

    /// <summary>
    /// Narrows the grid down via the search box. The grid is paged, so this is also how a test
    /// reaches an item that is not on the first page.
    /// </summary>
    public async Task SearchAsync(string term)
        => await page.Locator("[data-test='search']").FillAsync(term);

    public ILocator Row(string text) => Rows.Filter(new LocatorFilterOptions { HasText = text });

    public ILocator StatusOf(string text) => Row(text).Locator("[data-test='item-status']");

    public Task ClickLendAsync(string text) => Row(text).Locator("[data-test='lend-item']").ClickAsync();

    public Task ClickTakeBackAsync(string text) => Row(text).Locator("[data-test='return-item']").ClickAsync();

    public ILocator LendButtonOf(string text) => Row(text).Locator("[data-test='lend-item']");

    public Task ToggleLendableAsync(string text) => Row(text).Locator(".rz-switch").ClickAsync();

    public Task OpenDetailsAsync(string text) => Row(text).Locator("[data-test='show-details']").ClickAsync();

    public static async Task<int> NumberAsync(ILocator kpi) => int.Parse(await kpi.InnerTextAsync());
}
