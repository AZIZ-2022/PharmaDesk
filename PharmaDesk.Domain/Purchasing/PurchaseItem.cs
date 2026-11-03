namespace PharmaDesk.Domain.Purchasing;

public class PurchaseItem
{
    public long Id { get; set; }

    public long PurchaseId { get; set; }

    public long MedicineId { get; set; }

    public string BatchNumber { get; set; } = string.Empty;

    public DateOnly ExpiryDate { get; set; }

    public decimal QuantityPacks { get; set; }

    public decimal PurchasePricePerPack { get; set; }

    public decimal SellingPricePerBaseUnit { get; set; }

    public decimal LineTotal { get; set; }
}
