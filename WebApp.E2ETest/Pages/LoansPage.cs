namespace WebApp.E2ETest.Pages;

/// <summary>The list of open loans ("/loans").</summary>
public sealed class LoansPage(IPage page)
{
    public ILocator Rows => page.Locator("[data-test='loans-grid'] tbody tr.rz-data-row");

    public ILocator OpenLoans => page.Locator("[data-test='stat-open-loans']");

    public async Task OpenAsync()
    {
        await page.GotoAsync("/loans");
        await Rows.First.WaitForAsync();
    }

    public ILocator Row(string text) => Rows.Filter(new LocatorFilterOptions { HasText = text });

    public Task ClickTakeBackAsync(string text) => Row(text).Locator("[data-test='return-item']").ClickAsync();
}
