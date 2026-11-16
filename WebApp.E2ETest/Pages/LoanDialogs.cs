namespace WebApp.E2ETest.Pages;

/// <summary>The "lend item" dialog.</summary>
public sealed class LendDialog(IPage page)
{
    private ILocator Root => page.Locator("[data-test='lend-dialog']");

    public ILocator Error => Root.Locator("[data-test='dialog-error']");

    /// <summary>Lends the item to the first person of the list and returns that person's name.</summary>
    public async Task<string> LendToFirstPersonAsync()
    {
        await Root.WaitForAsync();
        var person = await page.PickFromDropDownAsync("lend-person");
        await page.Locator("[data-test='lend-confirm']").ClickAsync();
        await Root.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Detached });
        return person;
    }
}

/// <summary>The "take back item" dialog.</summary>
public sealed class ReturnDialog(IPage page)
{
    private ILocator Root => page.Locator("[data-test='return-dialog']");

    public async Task TakeBackAsync(bool damaged = false)
    {
        await Root.WaitForAsync();
        await page.PickFromDropDownAsync("return-staff");
        if (damaged)
            await page.Locator("[data-test='return-damaged']").ClickAsync();
        await page.Locator("[data-test='return-confirm']").ClickAsync();
        await Root.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Detached });
    }
}
