namespace WebApp.E2ETest.Pages;

/// <summary>
/// How to operate the Radzen components that are not plain HTML controls. Kept in one place:
/// if a Radzen update changes their markup, only this file has to follow.
/// </summary>
internal static class RadzenControls
{
    /// <summary>Opens a Radzen dropdown and picks the option with the given text (or the first one).</summary>
    /// <returns>The text of the option that was picked.</returns>
    public static async Task<string> PickFromDropDownAsync(this IPage page, string dataTest, string? optionText = null)
    {
        await page.Locator($"[data-test='{dataTest}']").ClickAsync();

        // The option list is rendered at the end of <body>, outside the dropdown element.
        var options = page.Locator(".rz-dropdown-panel:visible").GetByRole(AriaRole.Option);
        var option = optionText is null
            ? options.First
            : options.Filter(new LocatorFilterOptions { HasText = optionText }).First;

        // Read the text before the click: afterwards the list closes and re-renders.
        var pickedText = (await option.InnerTextAsync()).Trim();
        await option.ClickAsync();
        return pickedText;
    }

    /// <summary>The toast shown after an action; fails the test when it never appears.</summary>
    public static ILocator Notification(this IPage page, string text)
        => page.Locator(".rz-notification").Filter(new LocatorFilterOptions { HasText = text }).First;
}
