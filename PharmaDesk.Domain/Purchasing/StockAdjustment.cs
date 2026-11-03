namespace PharmaDesk.Domain.Purchasing;

public class StockAdjustment
{
    public long Id { get; set; }

    public long TenantId { get; set; }

    public long BranchId { get; set; }

    public long MedicineBatchId { get; set; }

    public decimal QuantityDelta { get; set; }

    public string Reason { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public DateTime AdjustedAtUtc { get; set; }
}
