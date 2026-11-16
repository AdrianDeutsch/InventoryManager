namespace WebApp.Components.Shared;

/// <summary>One place for how dates look, so every screen prints them the same way.</summary>
public static class Format
{
    public const string DatePickerFormat = "dd MMM yyyy";

    public static string Date(DateOnly date) => date.ToString(DatePickerFormat);

    public static string Date(DateOnly? date) => date is null ? "–" : Date(date.Value);
}
