using Radzen;

namespace WebApp.Components.Shared;

/// <summary>
/// Opens the lend and return dialogs. Three pages offer these actions, so the wiring between a
/// page and the dialogs lives here once.
/// </summary>
public class LoanDialogs(DialogService dialogService, NotificationService notificationService)
{
    private static readonly DialogOptions Options = new() { Width = "480px", CloseDialogOnOverlayClick = false };

    /// <returns><c>true</c> when the item was lent and the caller should reload its data.</returns>
    public async Task<bool> LendAsync(int itemId, string itemName, string serialNumber)
    {
        var lent = await dialogService.OpenAsync<LendDialog>("Lend item", new Dictionary<string, object?>
        {
            [nameof(LendDialog.ItemId)] = itemId,
            [nameof(LendDialog.ItemName)] = itemName,
            [nameof(LendDialog.SerialNumber)] = serialNumber,
        }, Options) is true;

        if (lent)
            notificationService.Notify(NotificationSeverity.Success, "Item lent", $"'{itemName}' was handed out.");
        return lent;
    }

    /// <returns><c>true</c> when the item was taken back and the caller should reload its data.</returns>
    public async Task<bool> ReturnAsync(int reservationId, string itemName, string personName, DateOnly loanDate)
    {
        var returned = await dialogService.OpenAsync<ReturnDialog>("Take back item", new Dictionary<string, object?>
        {
            [nameof(ReturnDialog.ReservationId)] = reservationId,
            [nameof(ReturnDialog.ItemName)] = itemName,
            [nameof(ReturnDialog.PersonName)] = personName,
            [nameof(ReturnDialog.LoanDate)] = loanDate,
        }, Options) is true;

        if (returned)
            notificationService.Notify(NotificationSeverity.Success, "Item returned", $"'{itemName}' was taken back.");
        return returned;
    }
}
