namespace WebApp.E2ETest.Pages;

/// <summary>The detail page of one item ("/inventory/{id}").</summary>
public sealed class ItemDetailPage(IPage page)
{
    public ILocator Heading => page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Level = 1 });
    public ILocator SerialNumber => page.Locator("[data-test='detail-serial']");
    public ILocator Status => page.Locator("[data-test='item-status']");
    public ILocator Borrower => page.Locator("[data-test='detail-borrower']");
    public ILocator LendButton => page.Locator("[data-test='lend-item']");
    public ILocator TakeBackButton => page.Locator("[data-test='return-item']");
    public ILocator HistoryRows => page.Locator("[data-test='loan-history'] tbody tr.rz-data-row");
    public ILocator NotFoundMessage => page.Locator("[data-test='item-not-found']");

    public Task OpenAsync(int id) => page.GotoAsync($"/inventory/{id}");
}
