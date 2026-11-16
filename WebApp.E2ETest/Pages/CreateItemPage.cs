namespace WebApp.E2ETest.Pages;

/// <summary>The "new item" form ("/inventory/create").</summary>
public sealed class CreateItemPage(IPage page)
{
    public ILocator ValidationMessages => page.Locator(".validation-message");

    public async Task OpenAsync()
    {
        await page.GotoAsync("/inventory/create");
        await page.Locator("[data-test='serial-number']").WaitForAsync();
    }

    public async Task FillAsync(string serialNumber, string itemName, string category)
    {
        await page.Locator("[data-test='serial-number']").FillAsync(serialNumber);
        await page.Locator("[data-test='item-name']").FillAsync(itemName);
        await page.PickFromDropDownAsync("category", category);
    }

    public Task SubmitAsync() => page.Locator("[data-test='create-item']").ClickAsync();

    /// <summary>Creates an item and waits until its detail page is shown.</summary>
    public async Task CreateAsync(string serialNumber, string itemName, string category = "Notebooks")
    {
        await OpenAsync();
        await FillAsync(serialNumber, itemName, category);
        await SubmitAsync();
        await page.WaitForURLAsync(new System.Text.RegularExpressions.Regex(@"/inventory/\d+$"));
    }
}
