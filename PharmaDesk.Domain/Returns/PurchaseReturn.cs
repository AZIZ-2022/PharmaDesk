namespace PharmaDesk.Domain.Returns;

public class PurchaseReturn
{
    public long Id { get; set; }

    public long TenantId { get; set; }

    public long BranchId { get; set; }

    public long PurchaseId { get; set; }

    public long MedicineBatchId { get; set; }

    public long SupplierId { get; set; }

    public decimal QuantityReturned { get; set; }

    public decimal CostPerBaseUnitAtPurchase { get; set; }

    public decimal ReturnAmount { get; set; }

    public string Reason { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public DateTime ReturnedAtUtc { get; set; }
}
