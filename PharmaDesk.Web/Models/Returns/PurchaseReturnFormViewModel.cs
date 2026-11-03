using System.ComponentModel.DataAnnotations;

namespace PharmaDesk.Web.Models.Returns;

public class PurchaseReturnFormViewModel
{
    public long MedicineBatchId { get; set; }

    public long PurchaseId { get; set; }

    public string MedicineBrandName { get; set; } = string.Empty;

    public string BatchNumber { get; set; } = string.Empty;

    public decimal CurrentQuantityRemaining { get; set; }

    public decimal CostPerBaseUnit { get; set; }

    [Range(0.01, 1000000)]
    [Display(Name = "Quantity returned to supplier")]
    public decimal QuantityReturned { get; set; }

    [Required]
    [Display(Name = "Reason")]
    public string Reason { get; set; } = string.Empty;

    [Display(Name = "Notes")]
    public string? Notes { get; set; }
}
