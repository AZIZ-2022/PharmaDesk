using System.ComponentModel.DataAnnotations;

namespace PharmaDesk.Web.Models.Returns;

public class SalesReturnFormViewModel
{
    public long SaleItemId { get; set; }

    public long SaleId { get; set; }

    public string MedicineBrandName { get; set; } = string.Empty;

    public decimal OriginalQuantity { get; set; }

    public decimal UnitPriceAtSale { get; set; }

    [Range(0.01, 1000000)]
    [Display(Name = "Quantity returned")]
    public decimal QuantityReturned { get; set; }

    [Display(Name = "Put this stock back into inventory")]
    public bool IsRestocked { get; set; } = true;

    [Required]
    [Display(Name = "Reason")]
    public string Reason { get; set; } = string.Empty;

    [Display(Name = "Notes")]
    public string? Notes { get; set; }
}
