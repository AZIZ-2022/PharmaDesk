using System.ComponentModel.DataAnnotations;

namespace PharmaDesk.Web.Models.Inventory;

public class StockAdjustmentFormViewModel
{
    public long MedicineBatchId { get; set; }

    public string MedicineBrandName { get; set; } = string.Empty;

    public string BatchNumber { get; set; } = string.Empty;

    public decimal CurrentQuantityRemaining { get; set; }

    [Required]
    [Display(Name = "Quantity adjustment (use a negative number to reduce stock)")]
    public decimal QuantityDelta { get; set; }

    [Required]
    [Display(Name = "Reason")]
    public string Reason { get; set; } = string.Empty;

    [Display(Name = "Notes")]
    public string? Notes { get; set; }
}
