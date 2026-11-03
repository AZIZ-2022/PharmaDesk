using System.ComponentModel.DataAnnotations;

namespace PharmaDesk.Web.Models.Purchasing;

public class PurchaseItemFormViewModel
{
    [Required]
    public long MedicineId { get; set; }

    [Required]
    public string BatchNumber { get; set; } = string.Empty;

    [Required]
    public DateOnly ExpiryDate { get; set; }

    [Range(0.01, 1000000)]
    public decimal QuantityPacks { get; set; }

    [Range(0.01, 10000000)]
    public decimal PurchasePricePerPack { get; set; }

    [Range(0.01, 10000000)]
    public decimal SellingPricePerBaseUnit { get; set; }
}
