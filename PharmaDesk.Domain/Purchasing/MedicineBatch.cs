namespace PharmaDesk.Domain.Purchasing;

public class MedicineBatch
{
    public long Id { get; set; }

    public long TenantId { get; set; }

    public long BranchId { get; set; }

    public long MedicineId { get; set; }

    public long SupplierId { get; set; }

    public long PurchaseId { get; set; }

    public long PurchaseItemId { get; set; }

    public string BatchNumber { get; set; } = string.Empty;

    public DateOnly ExpiryDate { get; set; }

    public decimal QuantityReceived { get; set; }

    public decimal QuantityRemaining { get; set; }

    public decimal PurchaseCostPerBaseUnit { get; set; }

    public decimal SellingPricePerBaseUnit { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
