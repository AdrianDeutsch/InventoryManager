using System.ComponentModel.DataAnnotations;
using Application.Commands;

namespace WebApp.ViewModels;

/// <summary>
/// Editable form model backing the "create inventory item" page.
/// Data annotations enforce the input rules (required fields, 1-255 character bounds,
/// a category selection) on the client before any value reaches the service layer.
/// </summary>
public class InventoryItemFormModel
{
    [Required(ErrorMessage = "Serial number is required.")]
    [StringLength(255, MinimumLength = 1, ErrorMessage = "Serial number must be between 1 and 255 characters.")]
    public string SerialNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Item name is required.")]
    [StringLength(255, MinimumLength = 1, ErrorMessage = "Item name must be between 1 and 255 characters.")]
    public string ItemName { get; set; } = string.Empty;

    // 0 is the "nothing selected" sentinel of the category dropdown, hence the lower bound of 1.
    [Range(1, int.MaxValue, ErrorMessage = "Please select a category.")]
    public int CategoryId { get; set; }

    public bool IsAvailable { get; set; } = true;

    /// <summary>
    /// Maps the validated form state onto the command consumed by <c>InventoryService</c>.
    /// </summary>
    public AddInventoryItemCmd ToCommand() =>
        new(SerialNumber.Trim(), ItemName.Trim(), CategoryId, IsAvailable);
}
