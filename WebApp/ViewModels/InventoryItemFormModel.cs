using System.ComponentModel.DataAnnotations;
using Application.Commands;

namespace WebApp.ViewModels;

/// <summary>
/// Form state of the "new item" page. The annotations give immediate feedback while typing;
/// the service checks the same input again, because the form is not its only caller.
/// </summary>
public class InventoryItemFormModel
{
    [Required(ErrorMessage = "Serial number is required.")]
    [StringLength(255, ErrorMessage = "Serial number must not be longer than 255 characters.")]
    public string SerialNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Item name is required.")]
    [StringLength(255, ErrorMessage = "Item name must not be longer than 255 characters.")]
    public string ItemName { get; set; } = string.Empty;

    // 0 means "nothing selected" in the category dropdown, hence the lower bound of 1.
    [Range(1, int.MaxValue, ErrorMessage = "Please select a category.")]
    public int CategoryId { get; set; }

    public bool IsAvailable { get; set; } = true;

    public AddInventoryItemCmd ToCommand() => new(SerialNumber, ItemName, CategoryId, IsAvailable);
}
