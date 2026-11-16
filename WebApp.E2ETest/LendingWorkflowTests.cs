using WebApp.E2ETest.Pages;
using static Microsoft.Playwright.Assertions;

namespace WebApp.E2ETest;

public class LendingWorkflowTests(AppFixture app) : E2ETestBase(app)
{
    [Fact]
    public Task NewItem_CanBeLentAndTakenBack() => RunAsync(async page =>
    {
        var serialNumber = UniqueSerialNumber();
        var itemName = $"Workflow Laptop {serialNumber}";
        var detail = new ItemDetailPage(page);
        var loans = new LoansPage(page);

        // A new item starts available and has no history.
        await new CreateItemPage(page).CreateAsync(serialNumber, itemName);
        await Expect(detail.Heading).ToHaveTextAsync(itemName);
        await Expect(detail.Status).ToHaveTextAsync("Available");
        await Expect(detail.HistoryRows).ToHaveCountAsync(0);

        // Lending shows who has it and replaces "Lend" with "Take back".
        await detail.LendButton.ClickAsync();
        var borrower = await new LendDialog(page).LendToFirstPersonAsync();
        await Expect(detail.Status).ToHaveTextAsync("On loan");
        await Expect(detail.Borrower).ToContainTextAsync(ToDisplayName(borrower));
        await Expect(detail.LendButton).ToHaveCountAsync(0);
        await Expect(detail.HistoryRows).ToHaveCountAsync(1);

        // The loan appears in the list of open loans and can be closed from there.
        await loans.OpenAsync();
        await Expect(loans.Row(itemName)).ToHaveCountAsync(1);
        await loans.ClickTakeBackAsync(itemName);
        await new ReturnDialog(page).TakeBackAsync();
        await Expect(loans.Row(itemName)).ToHaveCountAsync(0);

        // Back on the detail page the item is available again and the loan is in the history.
        var list = new InventoryListPage(page);
        await list.OpenAsync();
        await list.SearchAsync(serialNumber);
        await list.OpenDetailsAsync(itemName);
        await Expect(detail.Status).ToHaveTextAsync("Available");
        await Expect(detail.HistoryRows).ToHaveCountAsync(1);
        await Expect(detail.HistoryRows.First).ToContainTextAsync("Returned");
    });

    [Fact]
    public Task LendingFromTheList_UpdatesStatusAndCounters() => RunAsync(async page =>
    {
        var serialNumber = UniqueSerialNumber();
        var itemName = $"Counter Laptop {serialNumber}";
        await new CreateItemPage(page).CreateAsync(serialNumber, itemName);

        var list = new InventoryListPage(page);
        await list.OpenAsync();
        var availableBefore = await InventoryListPage.NumberAsync(list.Available);
        var onLoanBefore = await InventoryListPage.NumberAsync(list.OnLoan);

        await list.SearchAsync(serialNumber);
        await list.ClickLendAsync(itemName);
        await new LendDialog(page).LendToFirstPersonAsync();

        await Expect(page.Notification("Item lent")).ToBeVisibleAsync();
        await Expect(list.StatusOf(itemName)).ToHaveTextAsync("On loan");
        await Expect(list.Available).ToHaveTextAsync((availableBefore - 1).ToString());
        await Expect(list.OnLoan).ToHaveTextAsync((onLoanBefore + 1).ToString());

        // Clean up, so the counters are back where the next test expects them.
        await list.ClickTakeBackAsync(itemName);
        await new ReturnDialog(page).TakeBackAsync();
        await Expect(list.StatusOf(itemName)).ToHaveTextAsync("Available");
    });

    [Fact]
    public Task DamagedReturn_BlocksTheItemUntilStaffReleasesIt() => RunAsync(async page =>
    {
        var serialNumber = UniqueSerialNumber();
        var itemName = $"Fragile Adapter {serialNumber}";
        await new CreateItemPage(page).CreateAsync(serialNumber, itemName, category: "Adapters");

        var list = new InventoryListPage(page);
        await list.OpenAsync();
        await list.SearchAsync(serialNumber);
        await list.ClickLendAsync(itemName);
        await new LendDialog(page).LendToFirstPersonAsync();
        await Expect(list.StatusOf(itemName)).ToHaveTextAsync("On loan");

        await list.ClickTakeBackAsync(itemName);
        await new ReturnDialog(page).TakeBackAsync(damaged: true);

        // Damaged: no "Lend" button until someone releases the item again.
        await Expect(list.StatusOf(itemName)).ToHaveTextAsync("Blocked");
        await Expect(list.LendButtonOf(itemName)).ToHaveCountAsync(0);

        await list.ToggleLendableAsync(itemName);

        await Expect(page.Notification("Item released")).ToBeVisibleAsync();
        await Expect(list.StatusOf(itemName)).ToHaveTextAsync("Available");
        await Expect(list.LendButtonOf(itemName)).ToHaveCountAsync(1);
    });

    // The dropdown lists people as "Lastname Firstname (Class)", the pages show "Firstname Lastname".
    private static string ToDisplayName(string dropdownText)
    {
        var name = dropdownText[..dropdownText.LastIndexOf('(')].Trim();
        var separator = name.LastIndexOf(' ');
        return $"{name[(separator + 1)..]} {name[..separator]}";
    }
}
